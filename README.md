# Suppy Inventory Update API

Backend technical assessment project for processing high-volume product and inventory updates from multiple tenants.

## Goal

The API accepts product batch updates quickly, validates the request, stores a durable batch record, and processes the actual product updates asynchronously in the background. The design is tenant-aware, idempotent, and ready for RabbitMQ, Redis, PostgreSQL, and Docker-based local infrastructure.

Assessment checklist:

```text
docs/ASSESSMENT_REQUIREMENTS.md
```

## Request Flow

```text
Tenant client
  -> POST /api/products/batch-update
  -> tenant rate limit
  -> validation + tenant-scoped idempotency
  -> save ProductUpdateBatch + ProductUpdateBatchItems + OutboxMessage
  -> return 202 Accepted
  -> DB-backed background worker claims accepted batches
  -> upsert Products by TenantId + ItemId
  -> update batch status
```

## Architecture

```text
API
  -> Application commands/queries
  -> Domain aggregates
  -> Persistence + generic repository + Unit of Work
  -> DB-backed background worker
  -> Outbox/RabbitMQ optional integration events
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

Submit a batch:

```http
POST /api/products/batch-update
Idempotency-Key: tenant-1-batch-001
X-Tenant-Id: tenant_1
Content-Type: application/json

{
  "tenantId": "tenant_1",
  "items": [
    {
      "itemId": "123",
      "price": 100,
      "stock": 50,
      "metadata": {
        "source": "assessment-demo"
      }
    }
  ]
}
```

Check batch status:

```http
GET /api/products/batches/{batchId}
```

Retry failed batch items:

```http
POST /api/products/batches/{batchId}/retry
```

List final products for a tenant:

```http
GET /api/products?tenantId=tenant_1&page=1&pageSize=50
```

## Local Infrastructure

Full Docker dev stack:

```powershell
Copy-Item .\docker\dev\.env.example .\docker\dev\.env
docker compose --env-file .\docker\dev\.env -f .\docker\dev\docker-compose.yml up -d
```

Included services:

```text
PostgreSQL:  localhost:5434
Redis:       localhost:6379
RabbitMQ:    localhost:5672
RabbitMQ UI: http://localhost:15672
Keycloak:    http://localhost:8088
```

Full local infrastructure guide:

```text
docs/LOCAL_INFRASTRUCTURE.md
```

## Assessment Notes

- Tenant isolation is enforced by storing `TenantId` on tenant-owned tables.
- Product identity is tenant-scoped: `(TenantId, ItemId)`.
- Product listing is tenant-scoped and paginated.
- Request idempotency is tenant-scoped: `(TenantId, IdempotencyKey)`.
- Clients should send `Idempotency-Key`; if omitted, the API creates a deterministic payload hash fallback for the simplified assessment flow.
- Tenant rate limiting partitions traffic by `X-Tenant-Id`, JWT `tenant_id`, query `tenantId`, then IP fallback.
- Clients should send `X-Tenant-Id` so infrastructure can protect each tenant before request body processing.
- The batch endpoint returns `202 Accepted`; processing happens asynchronously.
- The local implementation uses a DB-backed worker. RabbitMQ can be enabled as an optional transport for integration events.
- Background processing is designed to be at-least-once, so product upserts and batch processing are idempotent.
- Failures are handled with retry state, outbox retry, and RabbitMQ dead-letter support.
- Failed or partially failed batches can be retried. Successful items remain processed; only failed items are reset and picked up by the worker again.

## Template Documentation

Foundation architecture details are still available in:

```text
docs/ARCHITECTURE_GUIDE.md
```
