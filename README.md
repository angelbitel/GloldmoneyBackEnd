# GoldmoneyBackend

API backend de GoldMoney construida con .NET 8, Clean Architecture, DDD, CQRS, MediatR, Entity Framework Core y SQL Server.

## Stack

- .NET 8
- ASP.NET Core Web API
- MediatR
- FluentValidation
- Entity Framework Core 8
- SQL Server
- JWT Bearer Authentication
- xUnit + FluentAssertions

## Estructura

```text
GoldmoneyBackend.sln

src/
├── GoldmoneyBackend.Api
├── GoldmoneyBackend.Application
├── GoldmoneyBackend.Data
├── GoldmoneyBackend.Domain
└── GoldmoneyBackend.Infrastructure

tests/
├── GoldmoneyBackend.Application.Tests
├── GoldmoneyBackend.Domain.Tests
└── GoldmoneyBackend.IntegrationTests
```

## Capas

### GoldmoneyBackend.Api

Expone controllers, autenticación, autorización, documentación OpenAPI, health check y manejo global de excepciones.

### GoldmoneyBackend.Application

Contiene casos de uso CQRS, handlers, DTOs, interfaces, validators y pipeline behaviors.

### GoldmoneyBackend.Domain

Contiene entidades, aggregate roots, value objects, eventos de dominio, enums y reglas de negocio.

### GoldmoneyBackend.Infrastructure

Contiene persistencia del agregado moderno, autenticación JWT y servicios técnicos.

### GoldmoneyBackend.Data

Contiene acceso a tablas legacy de la base de datos existente, especialmente `CLIENTES` y `EMPRESA`.

## Base de datos

La configuración de desarrollo actual apunta a SQL Server local:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=db_a6b594_sade;User Id=mcafeuser;Password=systemroot;TrustServerCertificate=True;Encrypt=False;"
  }
}
```

Tablas relevantes actualmente integradas:

- `ClientesBackend`: agregado moderno de clientes manejado por EF Core y migraciones.
- `CLIENTES`: tabla legacy para altas y actualizaciones directas.
- `EMPRESA`: tabla legacy para altas y actualizaciones directas.

## Cómo ejecutar

### Restaurar paquetes

```powershell
dotnet restore GoldmoneyBackend.sln
```

### Compilar

```powershell
dotnet build GoldmoneyBackend.sln
```

### Ejecutar la API

```powershell
dotnet run --project src/GoldmoneyBackend.Api/GoldmoneyBackend.Api.csproj
```

### Ejecutar tests

```powershell
dotnet test GoldmoneyBackend.sln
```

## Documentación API

En desarrollo están disponibles:

- Swagger UI
- Scalar API Reference

Además existe un health check:

```http
GET /health
```

## Autenticación JWT

La API expone login JWT en:

```http
POST /api/auth/login
```

Payload:

```json
{
  "userName": "admin",
  "password": "Admin123!"
}
```

Usuarios de desarrollo configurados actualmente:

- `admin` / `Admin123!`
- `manager` / `Manager123!`
- `analyst` / `Analyst123!`

Roles usados por autorización:

- `Admin`
- `Manager`
- `Analyst`

Policies principales:

- `AdminOnly`
- `Backoffice`
- `ClientesRead`
- `ClientesWrite`
- `ClientesDelete`

## Endpoints principales

### Auth

- `POST /api/auth/login`

### Clientes modernos

- `POST /api/clientes`
- `GET /api/clientes`
- `GET /api/clientes/{id}`
- `PUT /api/clientes/{id}`
- `DELETE /api/clientes/{id}`

### Clientes legacy en tabla CLIENTES

- `POST /api/clientes-db`
- `PUT /api/clientes-db/{idCliente}`

### Empresas legacy en tabla EMPRESA

- `POST /api/empresas`
- `PUT /api/empresas/{codigoEmpresa}`

## Notas de diseño

- Los controllers se mantienen delgados.
- La validación de entrada se ejecuta en Application con FluentValidation.
- Las reglas de negocio viven en Domain.
- Las consultas de lectura usan `AsNoTracking()` cuando aplica.
- El manejo global de errores transforma excepciones a `ProblemDetails`.
- El proyecto `GoldmoneyBackend.Data` se usa para integrarse con tablas legacy existentes sin romper el modelo moderno.

## Git

Repositorio remoto configurado:

```text
https://github.com/angelbitel/GloldmoneyBackEnd.git
```
