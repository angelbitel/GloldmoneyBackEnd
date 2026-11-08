<#
.SYNOPSIS
    Exporta la base del VB6 a un .bacpac importable en Azure SQL Database, sin modificar la base original.

.DESCRIPTION
    1. Hace una copia de seguridad COPY_ONLY de la base y la restaura con otro nombre en el mismo servidor.
    2. En la copia quita lo que Azure SQL Database no admite:
       - los procedimientos dt_* (restos de Visual SourceSafe de SQL Server 2000, usan sp_OA);
       - los usuarios de base de datos (Azure no acepta usuarios ligados a logins del servidor;
         se crean de nuevo en Azure si hacen falta).
    3. Exporta la copia a .bacpac con SqlPackage, que valida la compatibilidad con Azure.
    4. Borra la copia y su archivo de respaldo.

    Requiere sqlcmd y SqlPackage (dotnet tool install -g microsoft.sqlpackage), y permisos de
    BACKUP, RESTORE y CREATE DATABASE en el servidor de origen.

.EXAMPLE
    # Autenticación de Windows
    .\Exportar-BaseParaAzure.ps1 -Servidor localhost -Base db_a6b594_sade -Bacpac C:\temp\goldmoney.bacpac

.EXAMPLE
    # Usuario de SQL Server
    .\Exportar-BaseParaAzure.ps1 -Servidor 10.30.10.177 -Base db_a6b594_sade -Usuario sa -Bacpac C:\temp\goldmoney.bacpac
#>
param(
    [Parameter(Mandatory)] [string]$Servidor,
    [Parameter(Mandatory)] [string]$Base,
    [Parameter(Mandatory)] [string]$Bacpac,
    [string]$Usuario,
    [securestring]$Contrasena,
    [string]$BaseCopia = "$($Base)_ExportAzure"
)

$ErrorActionPreference = 'Stop'

function Get-Comando([string[]]$candidatos) {
    foreach ($c in $candidatos) {
        $cmd = Get-Command $c -ErrorAction SilentlyContinue
        if ($cmd) { return $cmd.Source }
        if (Test-Path $c) { return $c }
    }
    throw "No se encontró ninguno de: $($candidatos -join ', ')"
}

$sqlcmd = Get-Comando @('sqlcmd', 'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE')
$sqlpackage = Get-Comando @('sqlpackage', "$env:USERPROFILE\.dotnet\tools\sqlpackage.exe")

if ($Usuario -and -not $Contrasena) {
    $Contrasena = Read-Host "Contraseña de $Usuario" -AsSecureString
}
$textoContrasena = if ($Contrasena) { [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($Contrasena)) }

# @(...): un if/else que devuelve un solo elemento lo desenvuelve a texto y el splatting lo rompe.
$autenticacion = @(if ($Usuario) { '-U', $Usuario, '-P', $textoContrasena } else { '-E' })

function Invoke-Sql([string]$baseDatos, [string]$consulta) {
    # Por archivo (-i): PowerShell 5.1 parte mal los argumentos con espacios y saltos de línea.
    $archivo = [IO.Path]::GetTempFileName()
    try {
        Set-Content -Path $archivo -Value "SET NOCOUNT ON;`r`n$consulta" -Encoding UTF8
        $salida = & $sqlcmd -S $Servidor @autenticacion -C -b -d $baseDatos -h -1 -W -i $archivo
        if ($LASTEXITCODE -ne 0) { throw "Falló la consulta en $baseDatos`n$($salida -join "`n")" }
        return $salida
    }
    finally { Remove-Item $archivo -ErrorAction SilentlyContinue }
}

$existeCopia = (Invoke-Sql 'master' "SELECT COUNT(*) FROM sys.databases WHERE name = N'$BaseCopia'") -join ''
if ([int]$existeCopia.Trim() -gt 0) {
    throw "Ya existe una base '$BaseCopia' en $Servidor. Bórrela o use -BaseCopia con otro nombre."
}

$carpetaRespaldo = ((Invoke-Sql 'master' "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(400))") -join '').Trim()
$carpetaDatos = ((Invoke-Sql 'master' "SELECT CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS nvarchar(400))") -join '').Trim()
$archivoRespaldo = Join-Path $carpetaRespaldo "$BaseCopia.bak"

try {
    Write-Host "[1/4] Copiando '$Base' como '$BaseCopia' (la original no se modifica)..."
    $archivos = Invoke-Sql $Base "SELECT name + '|' + CAST(type AS varchar(1)) FROM sys.database_files"
    $mover = ($archivos | Where-Object { $_.Trim() } | ForEach-Object {
        $nombre, $tipo = $_.Trim().Split('|')
        $extension = if ($tipo -eq '1') { '_log.ldf' } else { '.mdf' }
        "MOVE N'$nombre' TO N'$(Join-Path $carpetaDatos "$BaseCopia`_$nombre$extension")'"
    }) -join ', '

    Invoke-Sql 'master' "BACKUP DATABASE [$Base] TO DISK = N'$archivoRespaldo' WITH COPY_ONLY, INIT, FORMAT;" | Out-Null
    Invoke-Sql 'master' "RESTORE DATABASE [$BaseCopia] FROM DISK = N'$archivoRespaldo' WITH $mover;" | Out-Null

    Write-Host "[2/4] Quitando de la copia lo que Azure SQL no admite..."
    $quitados = Invoke-Sql $BaseCopia @"
DECLARE @sql nvarchar(max) = N'';

-- Procedimientos dt_* de SQL Server 2000 (Visual SourceSafe), que usan sp_OA.
SELECT @sql += N'DROP PROCEDURE ' + QUOTENAME(SCHEMA_NAME(schema_id)) + N'.' + QUOTENAME(name) + N'; '
FROM sys.procedures WHERE name LIKE N'dt[_]%';

-- Usuarios de base de datos: sus esquemas pasan a dbo y se eliminan.
SELECT @sql += N'ALTER AUTHORIZATION ON SCHEMA::' + QUOTENAME(s.name) + N' TO dbo; '
FROM sys.schemas s JOIN sys.database_principals p ON p.principal_id = s.principal_id
WHERE p.type IN ('S', 'U', 'G') AND p.principal_id > 4;

SELECT @sql += N'DROP USER ' + QUOTENAME(name) + N'; '
FROM sys.database_principals WHERE type IN ('S', 'U', 'G') AND principal_id > 4;

EXEC sp_executesql @sql;
SELECT @sql;
"@
    Write-Host "      $($quitados -join ' ')"

    Write-Host "[3/4] Exportando a $Bacpac (SqlPackage valida la compatibilidad con Azure)..."
    $carpetaBacpac = Split-Path -Parent $Bacpac
    if ($carpetaBacpac -and -not (Test-Path $carpetaBacpac)) { New-Item -ItemType Directory -Force -Path $carpetaBacpac | Out-Null }

    $origen = @(if ($Usuario) {
        "/SourceServerName:$Servidor", "/SourceDatabaseName:$BaseCopia", "/SourceUser:$Usuario", "/SourcePassword:$textoContrasena", '/SourceTrustServerCertificate:True'
    } else {
        "/SourceConnectionString:Server=$Servidor;Database=$BaseCopia;Integrated Security=True;TrustServerCertificate=True"
    })
    & $sqlpackage /Action:Export @origen "/TargetFile:$Bacpac" /OverwriteFiles:True
    if ($LASTEXITCODE -ne 0) { throw "SqlPackage no pudo exportar la base." }
}
finally {
    Write-Host "[4/4] Borrando la copia temporal..."
    try { Invoke-Sql 'master' "IF DB_ID(N'$BaseCopia') IS NOT NULL BEGIN ALTER DATABASE [$BaseCopia] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$BaseCopia]; END" | Out-Null }
    catch { Write-Warning "No se pudo borrar la base '$BaseCopia': $_" }
    # xp_delete_files existe desde SQL Server 2019; en versiones anteriores hay que borrarlo a mano.
    try { Invoke-Sql 'master' "EXEC master.sys.xp_delete_files N'$archivoRespaldo'" | Out-Null }
    catch { Write-Warning "Borre a mano el respaldo temporal en el servidor: $archivoRespaldo" }
}

Write-Host "Listo: $Bacpac ($([math]::Round((Get-Item $Bacpac).Length / 1MB, 1)) MB)"
