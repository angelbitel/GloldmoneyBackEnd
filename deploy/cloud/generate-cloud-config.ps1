param(
    [string]$EnvFile = "c:\GloldmoneyBackEnd\deploy\cloud\.env.cloud"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $EnvFile)) {
    throw "Env file not found: $EnvFile"
}

function Get-EnvMap([string]$path) {
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

$envMap = Get-EnvMap $EnvFile

$required = @(
    "PROJECT_NAME", "API_SERVICE_NAME", "DB_SERVICE_NAME", "DB_NAME", "DB_USER", "SA_PASSWORD",
    "JWT_KEY", "JWT_ISSUER", "JWT_AUDIENCE", "JWT_EXPIRES_MINUTES", "API_PORT",
  "ASPNETCORE_ENVIRONMENT", "RENDER_API_PLAN", "RENDER_DB_PLAN", "RENDER_DB_DISK_GB"
)

foreach ($key in $required) {
    if (-not $envMap.ContainsKey($key) -or [string]::IsNullOrWhiteSpace($envMap[$key])) {
        throw "Missing required key in env file: $key"
    }
}

$connectionString = "Server=$($envMap.DB_SERVICE_NAME),1433;Database=$($envMap.DB_NAME);User Id=$($envMap.DB_USER);Password=$($envMap.SA_PASSWORD);TrustServerCertificate=True;Encrypt=False;"

$renderOut = @"
services:
  - type: pserv
    name: $($envMap.DB_SERVICE_NAME)
    env: docker
    dockerfilePath: ./deploy/render/mssql.Dockerfile
    plan: $($envMap.RENDER_DB_PLAN)
    envVars:
      - key: ACCEPT_EULA
        value: Y
      - key: MSSQL_PID
        value: Developer
      - key: MSSQL_SA_PASSWORD
        value: $($envMap.SA_PASSWORD)
    disk:
      name: $($envMap.PROJECT_NAME)-sql-data
      mountPath: /var/opt/mssql
      sizeGB: $($envMap.RENDER_DB_DISK_GB)

  - type: web
    name: $($envMap.API_SERVICE_NAME)
    env: docker
    dockerfilePath: ./deploy/render/api.Dockerfile
    dockerContext: .
    plan: $($envMap.RENDER_API_PLAN)
    healthCheckPath: /health
    envVars:
      - key: ASPNETCORE_ENVIRONMENT
        value: $($envMap.ASPNETCORE_ENVIRONMENT)
      - key: ASPNETCORE_URLS
        value: http://+:8080
      - key: ConnectionStrings__DefaultConnection
        value: $connectionString
      - key: Jwt__Key
        value: $($envMap.JWT_KEY)
      - key: Jwt__Issuer
        value: $($envMap.JWT_ISSUER)
      - key: Jwt__Audience
        value: $($envMap.JWT_AUDIENCE)
      - key: Jwt__ExpiresMinutes
        value: "$($envMap.JWT_EXPIRES_MINUTES)"
"@

$renderPath = "c:\GloldmoneyBackEnd\render.generated.yaml"
Set-Content -Path $renderPath -Value $renderOut -Encoding UTF8

Write-Output "Generated: $renderPath"
Write-Output "Azure artifact generation is temporarily disabled."
