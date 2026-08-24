param(
    [string]$EnvFile = "c:\GloldmoneyBackEnd\deploy\azure\.env.azure",
    [string]$ProjectPath = "c:\GloldmoneyBackEnd\src\GoldmoneyBackend.Api\GoldmoneyBackend.Api.csproj"
)

$ErrorActionPreference = "Stop"

function Get-EnvMap([string]$path) {
    if (-not (Test-Path $path)) {
        throw "Env file not found: $path"
    }

    $map = @{}
    Get-Content $path | ForEach-Object {
        $line = $_.Trim()
        if (-not $line -or $line.StartsWith("#")) { return }
        $parts = $line -split "=", 2
        if ($parts.Count -eq 2) {
            $map[$parts[0].Trim()] = $parts[1].Trim()
        }
    }

    return $map
}

function Get-Bool([hashtable]$map, [string]$key, [bool]$defaultValue) {
    if (-not $map.ContainsKey($key)) { return $defaultValue }
    $raw = $map[$key]
    if ([string]::IsNullOrWhiteSpace($raw)) { return $defaultValue }
    switch ($raw.ToLowerInvariant()) {
        "1" { return $true }
        "true" { return $true }
        "yes" { return $true }
        "y" { return $true }
        "0" { return $false }
        "false" { return $false }
        "no" { return $false }
        "n" { return $false }
        default { throw "Invalid boolean value for $key: $raw" }
    }
}

function Require-Keys([hashtable]$map, [string[]]$keys) {
    foreach ($key in $keys) {
        if (-not $map.ContainsKey($key) -or [string]::IsNullOrWhiteSpace($map[$key])) {
            throw "Missing required key: $key"
        }
    }
}

function Require-Command([string]$name) {
    if (-not (Get-Command $name -ErrorAction SilentlyContinue)) {
        throw "Required command not found: $name"
    }
}

$envMap = Get-EnvMap $EnvFile

Require-Keys $envMap @(
    "SUBSCRIPTION_ID", "LOCATION", "AZ_RESOURCE_GROUP", "APP_PLAN_NAME", "WEBAPP_NAME", "APP_SERVICE_SKU",
    "SQL_SERVER_NAME", "SQL_DB_NAME", "SQL_ADMIN_USER", "SQL_ADMIN_PASSWORD",
    "JWT_KEY", "JWT_ISSUER", "JWT_AUDIENCE", "JWT_EXPIRES_MINUTES", "ASPNETCORE_ENVIRONMENT"
)

$migrateData = Get-Bool $envMap "MIGRATE_DATA" $false
$addClientIp = Get-Bool $envMap "ADD_CLIENT_IP_FIREWALL" $true

Require-Command "az"
Require-Command "dotnet"

if ($migrateData) {
    Require-Keys $envMap @(
        "SOURCE_SQL_SERVER", "SOURCE_SQL_DB", "SOURCE_SQL_USER", "SOURCE_SQL_PASSWORD", "BACPAC_PATH"
    )
    if (-not (Get-Command "SqlPackage" -ErrorAction SilentlyContinue)) {
        throw "MIGRATE_DATA=true requires SqlPackage command in PATH."
    }
}

Write-Host "[1/8] Selecting Azure subscription..."
az account set --subscription $envMap.SUBSCRIPTION_ID | Out-Null

Write-Host "[2/8] Creating resource group..."
az group create --name $envMap.AZ_RESOURCE_GROUP --location $envMap.LOCATION | Out-Null

Write-Host "[3/8] Creating Azure SQL logical server and database..."
az sql server create `
  --name $envMap.SQL_SERVER_NAME `
  --resource-group $envMap.AZ_RESOURCE_GROUP `
  --location $envMap.LOCATION `
  --admin-user $envMap.SQL_ADMIN_USER `
  --admin-password $envMap.SQL_ADMIN_PASSWORD | Out-Null

az sql db create `
  --resource-group $envMap.AZ_RESOURCE_GROUP `
  --server $envMap.SQL_SERVER_NAME `
  --name $envMap.SQL_DB_NAME `
  --service-objective Basic | Out-Null

Write-Host "[4/8] Configuring SQL firewall rules..."
az sql server firewall-rule create `
  --resource-group $envMap.AZ_RESOURCE_GROUP `
  --server $envMap.SQL_SERVER_NAME `
  --name AllowAzureServices `
  --start-ip-address 0.0.0.0 `
  --end-ip-address 0.0.0.0 | Out-Null

if ($addClientIp) {
    $myIp = Invoke-RestMethod -Uri "https://api.ipify.org"
    az sql server firewall-rule create `
      --resource-group $envMap.AZ_RESOURCE_GROUP `
      --server $envMap.SQL_SERVER_NAME `
      --name AllowMyCurrentIP `
      --start-ip-address $myIp `
      --end-ip-address $myIp | Out-Null
}

if ($migrateData) {
    Write-Host "[5/8] Exporting BACPAC from source SQL (local/legacy)..."
    $bacpacDir = Split-Path -Parent $envMap.BACPAC_PATH
    if (-not (Test-Path $bacpacDir)) {
        New-Item -ItemType Directory -Force -Path $bacpacDir | Out-Null
    }

    SqlPackage /Action:Export `
      /SourceServerName:$envMap.SOURCE_SQL_SERVER `
      /SourceDatabaseName:$envMap.SOURCE_SQL_DB `
      /SourceUser:$envMap.SOURCE_SQL_USER `
      /SourcePassword:$envMap.SOURCE_SQL_PASSWORD `
      /TargetFile:$envMap.BACPAC_PATH | Out-Null

    Write-Host "[6/8] Importing BACPAC to Azure SQL..."
    SqlPackage /Action:Import `
      /TargetServerName:"tcp:$($envMap.SQL_SERVER_NAME).database.windows.net,1433" `
      /TargetDatabaseName:$envMap.SQL_DB_NAME `
      /TargetUser:$envMap.SQL_ADMIN_USER `
      /TargetPassword:$envMap.SQL_ADMIN_PASSWORD `
      /SourceFile:$envMap.BACPAC_PATH | Out-Null
}

Write-Host "[7/8] Creating App Service and deploying API..."
az appservice plan create `
  --name $envMap.APP_PLAN_NAME `
  --resource-group $envMap.AZ_RESOURCE_GROUP `
  --sku $envMap.APP_SERVICE_SKU `
  --is-linux | Out-Null

az webapp create `
  --resource-group $envMap.AZ_RESOURCE_GROUP `
  --plan $envMap.APP_PLAN_NAME `
  --name $envMap.WEBAPP_NAME `
  --runtime "DOTNETCORE|8.0" | Out-Null

$publishPath = "c:\GloldmoneyBackEnd\publish-azure"
$zipPath = "c:\GloldmoneyBackEnd\publish-azure.zip"
if (Test-Path $publishPath) { Remove-Item -Recurse -Force $publishPath }
if (Test-Path $zipPath) { Remove-Item -Force $zipPath }

dotnet publish $ProjectPath -c Release -o $publishPath | Out-Null
Compress-Archive -Path "$publishPath\*" -DestinationPath $zipPath -Force

az webapp deployment source config-zip `
  --resource-group $envMap.AZ_RESOURCE_GROUP `
  --name $envMap.WEBAPP_NAME `
  --src $zipPath | Out-Null

$conn = "Server=tcp:$($envMap.SQL_SERVER_NAME).database.windows.net,1433;Initial Catalog=$($envMap.SQL_DB_NAME);Persist Security Info=False;User ID=$($envMap.SQL_ADMIN_USER);Password=$($envMap.SQL_ADMIN_PASSWORD);MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

az webapp config appsettings set `
  --resource-group $envMap.AZ_RESOURCE_GROUP `
  --name $envMap.WEBAPP_NAME `
  --settings `
  "ConnectionStrings__DefaultConnection=$conn" `
  "Jwt__Key=$($envMap.JWT_KEY)" `
  "Jwt__Issuer=$($envMap.JWT_ISSUER)" `
  "Jwt__Audience=$($envMap.JWT_AUDIENCE)" `
  "Jwt__ExpiresMinutes=$($envMap.JWT_EXPIRES_MINUTES)" `
  "ASPNETCORE_ENVIRONMENT=$($envMap.ASPNETCORE_ENVIRONMENT)" | Out-Null

az webapp restart --resource-group $envMap.AZ_RESOURCE_GROUP --name $envMap.WEBAPP_NAME | Out-Null

Write-Host "[8/8] Validating health endpoint..."
$healthUrl = "https://$($envMap.WEBAPP_NAME).azurewebsites.net/health"
try {
    $response = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 60
    Write-Host "Health check status: $($response.StatusCode)"
}
catch {
    Write-Warning "Health check failed initially. App may still be warming up. URL: $healthUrl"
}

Write-Host "Deployment completed."
Write-Host "App URL: https://$($envMap.WEBAPP_NAME).azurewebsites.net"
