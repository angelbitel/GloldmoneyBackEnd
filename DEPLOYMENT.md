# Docker Deployment (API + SQL Server)

This repository includes Docker assets to run the API and SQL Server together and keep deployment portable for Azure and Render.

> Azure deployment is temporarily disabled while Render deployment is stabilized.

## Files

- docker-compose.yml
- docker-compose.prod.yml
- src/GoldmoneyBackend.Api/Dockerfile
- .env.example
- .env.prod.example
- render.yaml
- render.generated.yaml (generated)
- deploy/render/mssql.Dockerfile
- deploy/cloud/.env.cloud.example
- deploy/cloud/generate-cloud-config.ps1
- deploy/azure/azure.generated.env (generated, Azure paused)
- deploy/azure/deploy-containerapps.ps1.disabled
- deploy/azure/deploy-azure.ps1.disabled

## Local Run

1. Copy `.env.example` to `.env` and set secure values.
2. Start containers:

```bash
docker compose up --build -d
```

3. API URLs:

- http://localhost:5288
- http://localhost:5288/health

4. Stop containers:

```bash
docker compose down
```

To remove DB data volume:

```bash
docker compose down -v
```

## Local API Run (without hardcoded secrets)

Set the database connection string as an environment variable before running the API:

```powershell
$env:ConnectionStrings__DefaultConnection="Server=localhost,14335;Database=db_a6b594_sade;User Id=sa;Password=<YOUR_SA_PASSWORD>;TrustServerCertificate=True;Encrypt=False;"
dotnet run --project .\src\GoldmoneyBackend.Api\GoldmoneyBackend.Api.csproj
```

This value overrides `appsettings*.json` and avoids storing real passwords in source files.

## Production-like Run (single VM)

1. Copy `.env.prod.example` to `.env.prod` and set secure values.
2. Start stack:

```bash
docker compose -f docker-compose.prod.yml --env-file .env.prod up --build -d
```

3. Stop stack:

```bash
docker compose -f docker-compose.prod.yml --env-file .env.prod down
```

Notes:

- DB is not published to the host in `docker-compose.prod.yml`.
- API is exposed on `${API_PORT}` (defaults to `8080`).
- Restart policy is `unless-stopped` for both services.

## Azure

Azure deployment is temporarily paused.

- Scripts are disabled in `deploy/azure/*.disabled`.
- Re-enable when Render deployment is confirmed stable.

## Render

Use `render.yaml` Blueprint.

Important:

- Set `ConnectionStrings__DefaultConnection` manually as a secret in Render.
- Set `Jwt__Key` manually as a secret in Render.
- Ensure DB and API use the same SQL credentials.
- Prefer managed Render Postgres/MySQL alternatives when possible; SQL Server container is heavier operationally.

Suggested connection string format:

```text
Server=<render-db-host>,1433;Database=db_a6b594_sade;User Id=sa;Password=<SA_PASSWORD>;TrustServerCertificate=True;Encrypt=False;
```

## Unified Cloud Config (Render)

Use one config file and generate Render artifacts automatically.

1. Copy the template:

```powershell
Copy-Item .\deploy\cloud\.env.cloud.example .\deploy\cloud\.env.cloud
```

2. Fill values in `.env.cloud` once.

3. Generate platform files:

```powershell
.\deploy\cloud\generate-cloud-config.ps1
```

Generated outputs:

- `render.generated.yaml` (for Render Blueprint)

### Render deploy

- In Render, use `render.generated.yaml` as your Blueprint.

Azure generation/deploy steps are disabled for now.

## Security Notes

- Never commit real passwords or JWT secrets.
- Replace all sample values in `.env` and cloud secrets.
- For production, prefer managed SQL services where possible.
