param(
    [string]$EnvFile = "c:\GloldmoneyBackEnd\deploy\azure\azure.generated.env",
    [string]$ApiImage,
    [string]$DbImage = "mcr.microsoft.com/mssql/server:2022-latest"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $EnvFile)) {
    throw "Env file not found: $EnvFile"
}

if ([string]::IsNullOrWhiteSpace($ApiImage)) {
    throw "ApiImage is required. Example: -ApiImage myregistry.azurecr.io/goldmoney-api:latest"
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

az group create --name $envMap.AZ_RESOURCE_GROUP --location $envMap.LOCATION | Out-Null
az containerapp env create --name $envMap.AZ_CONTAINERAPPS_ENV --resource-group $envMap.AZ_RESOURCE_GROUP --location $envMap.LOCATION | Out-Null

az containerapp create `
  --name $envMap.DB_SERVICE_NAME `
  --resource-group $envMap.AZ_RESOURCE_GROUP `
  --environment $envMap.AZ_CONTAINERAPPS_ENV `
  --image $DbImage `
  --target-port 1433 `
  --ingress internal `
    --env-vars ACCEPT_EULA=Y MSSQL_PID=Developer MSSQL_SA_PASSWORD="$($envMap.SA_PASSWORD)" | Out-Null

az containerapp create `
  --name $envMap.API_SERVICE_NAME `
  --resource-group $envMap.AZ_RESOURCE_GROUP `
  --environment $envMap.AZ_CONTAINERAPPS_ENV `
  --image $ApiImage `
  --target-port 8080 `
  --ingress external `
  --env-vars ASPNETCORE_ENVIRONMENT=$($envMap.ASPNETCORE_ENVIRONMENT) ASPNETCORE_URLS=http://+:8080 ConnectionStrings__DefaultConnection="$($envMap.CONNECTIONSTRING_DEFAULT)" Jwt__Key="$($envMap.JWT_KEY)" Jwt__Issuer="$($envMap.JWT_ISSUER)" Jwt__Audience="$($envMap.JWT_AUDIENCE)" Jwt__ExpiresMinutes="$($envMap.JWT_EXPIRES_MINUTES)" | Out-Null

Write-Output "Azure Container Apps deployment completed."
