# Suppy Inventory Update API

Backend technical assessment project for processing high-volume product and inventory updates from multiple tenants.

## Goal

The API accepts product batch updates quickly, validates the request, stores a durable batch record, and processes the actual product updates asynchronously in the background. The design is tenant-aware, idempotent, and ready for RabbitMQ, Redis, PostgreSQL, and Docker-based local infrastructure.

Assessment checklist:

```text
docs/ASSESSMENT_REQUIREMENTS.md
```

## Planned Request Flow

```text
Tenant client
  -> POST /api/products/batch-update
  -> validation + tenant-scoped idempotency
  -> save ProductUpdateBatch + ProductUpdateBatchItems + OutboxMessage
  -> return 202 Accepted
  -> outbox dispatcher publishes integration event
  -> RabbitMQ consumer processes batch
  -> upsert Products by TenantId + ItemId
  -> update batch status
```

## Architecture

```text
API
  -> Application commands/queries
  -> Domain aggregates
  -> Persistence + generic repository + Unit of Work
  -> Outbox
  -> RabbitMQ background consumer
  -> PostgreSQL

Redis is used for cache, distributed locks, and idempotency support.
```

## Projects

- `Suppy.InventoryUpdate.Api` - Host/API, Swagger, middleware, security, infrastructure wiring
- `Suppy.InventoryUpdate.Api.Application` - use cases, validation, pipeline behaviors
- `Suppy.InventoryUpdate.Api.Domain` - aggregates, value objects, domain rules
- `Suppy.InventoryUpdate.Api.Abstractions` - shared contracts and interfaces
- `Suppy.InventoryUpdate.Api.GenericRepo` - EF/Mongo generic repository implementations
- `Suppy.InventoryUpdate.Api.Persistence` - EF DbContext, mappings, migrations, outbox store

## Local Setup

Create PostgreSQL databases:

```sql
CREATE DATABASE suppy_inventory_update;
CREATE DATABASE suppy_inventory_keycloak;
```

Apply migrations:

```bash
dotnet ef database update --configuration Debug --project Suppy.InventoryUpdate.Api.Persistence --startup-project Suppy.InventoryUpdate.Api --context AppDbContext
```

Run API:

```bash
dotnet run --project Suppy.InventoryUpdate.Api
```

Swagger:

```text
http://localhost:5253/swagger
```

## Local Infrastructure

Redis:

```bash
docker compose -f docker/redis/docker-compose.yml up -d
```

RabbitMQ:

```bash
docker compose -f docker/rabbitmq/docker-compose.yml up -d
```

RabbitMQ UI:

```text
http://localhost:15672
```

Keycloak:

```bash
docker compose -f docker/keycloak/docker-compose.yml up -d
```

Keycloak URL:

```text
http://localhost:8088
```

## Assessment Notes

- Tenant isolation is enforced by storing `TenantId` on tenant-owned tables.
- Product identity is tenant-scoped: `(TenantId, ItemId)`.
- Request idempotency is tenant-scoped: `(TenantId, IdempotencyKey)`.
- The batch endpoint should return `202 Accepted`; processing happens asynchronously.
- Background delivery is at-least-once, so product upserts and batch processing must be idempotent.
- Failures are handled with retry state, outbox retry, and RabbitMQ dead-letter support.

## Template Documentation

Foundation architecture details are still available in:

```text
docs/ARCHITECTURE_GUIDE.md
```
