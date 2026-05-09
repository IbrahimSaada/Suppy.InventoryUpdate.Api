# Local Infrastructure Guide

This project can run with only PostgreSQL for the core assessment flow. Redis, RabbitMQ, and Keycloak are included as optional local infrastructure to demonstrate production-readiness.

## Services

The full dev stack lives in:

```text
docker/dev/docker-compose.yml
```

It starts:

- PostgreSQL for application data and Keycloak data
- Redis for cache, locks, and idempotency infrastructure
- RabbitMQ with management UI
- Keycloak backed by PostgreSQL

## Ports

Default ports:

```text
PostgreSQL:  localhost:5434
Redis:       localhost:6379
RabbitMQ:    localhost:5672
RabbitMQ UI: http://localhost:15672
Keycloak:    http://localhost:8088
```

PostgreSQL uses `5434` by default to avoid conflicting with a local PostgreSQL installation on `5432`.

## Start Full Stack

From the repository root:

```powershell
Copy-Item .\docker\dev\.env.example .\docker\dev\.env
docker compose --env-file .\docker\dev\.env -f .\docker\dev\docker-compose.yml up -d
```

Check containers:

```powershell
docker compose --env-file .\docker\dev\.env -f .\docker\dev\docker-compose.yml ps
```

## Database Connection

The Docker PostgreSQL connection string is:

```text
Host=localhost;Port=5434;Database=suppy_inventory_update;Username=postgres;Password=ibrahim
```

If you use the Docker PostgreSQL port, override the app connection string before applying migrations or running the API:

```powershell
$env:ConnectionStrings__Postgres="Host=localhost;Port=5434;Database=suppy_inventory_update;Username=postgres;Password=ibrahim"
```

If you use your own local PostgreSQL on port `5432`, keep the value in `appsettings.Development.json`.

## Apply Migrations

```powershell
$env:ConnectionStrings__Postgres="Host=localhost;Port=5434;Database=suppy_inventory_update;Username=postgres;Password=ibrahim"
dotnet ef database update --configuration Debug --project .\Suppy.InventoryUpdate.Api.Persistence --startup-project .\Suppy.InventoryUpdate.Api --context AppDbContext
```

## Run API

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
$env:ConnectionStrings__Postgres="Host=localhost;Port=5434;Database=suppy_inventory_update;Username=postgres;Password=ibrahim"
dotnet run --project .\Suppy.InventoryUpdate.Api
```

Swagger:

```text
http://localhost:5253/swagger
```

Health checks:

```http
GET http://localhost:5253/health/live
GET http://localhost:5253/health/ready
```

## Optional Redis

Redis is disabled by default in development.

To enable Redis for local testing:

```powershell
$env:Redis__Enabled="true"
$env:Redis__ConnectionString="localhost:6379"
```

## Optional RabbitMQ

RabbitMQ is disabled by default in development.

To process product batches through RabbitMQ instead of the DB-backed worker:

```powershell
$env:ProductBatchProcessing__Enabled="false"
$env:Redis__Enabled="true"
$env:Redis__ConnectionString="localhost:6379"
$env:Messaging__Provider="RabbitMq"
$env:Messaging__RabbitMq__HostName="localhost"
$env:Messaging__RabbitMq__Port="5672"
$env:Messaging__RabbitMq__UserName="guest"
$env:Messaging__RabbitMq__Password="guest"
$env:Messaging__RabbitMq__ConsumerEnabled="true"
```

Why Redis is enabled here:

```text
RabbitMQ delivery is at-least-once.
Redis stores consumer idempotency state.
Duplicate broker deliveries can be acknowledged safely without duplicate processing.
```

RabbitMQ UI:

```text
http://localhost:15672
```

Default login:

```text
guest / guest
```

## Optional Keycloak

Keycloak is included for production-style authentication, but the assessment flow runs with authentication disabled by default.

Keycloak URL:

```text
http://localhost:8088
```

Bootstrap admin from `.env.example`:

```text
admin / ibrahim
```

After first login, create a permanent admin user and remove bootstrap credentials from `docker/dev/.env` before recreating the container.

Recommended realm name:

```text
suppy-inventory
```

## Stop Stack

Stop containers but keep volumes:

```powershell
docker compose --env-file .\docker\dev\.env -f .\docker\dev\docker-compose.yml down
```

Stop containers and remove volumes:

```powershell
docker compose --env-file .\docker\dev\.env -f .\docker\dev\docker-compose.yml down -v
```

Use `down -v` only when you intentionally want to delete local PostgreSQL/RabbitMQ data.
