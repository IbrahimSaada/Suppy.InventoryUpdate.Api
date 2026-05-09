# Suppy Inventory Update Architecture

## Assessment Problem

Multiple tenants send high-volume product updates. Each update contains an `itemId`, `price`, `stock`, and optional metadata. The backend must accept large batches without blocking the API request, avoid duplicate processing, keep product data consistent, and isolate tenants from each other.

## Proposed Architecture

```text
Tenant client
  -> API endpoint
  -> validation
  -> tenant-scoped idempotency
  -> batch persistence
  -> outbox persistence
  -> 202 Accepted
  -> outbox dispatcher
  -> RabbitMQ
  -> background consumer
  -> product upsert
  -> batch status update
```

## Dependency Direction

```text
API/Host -> Application -> Domain
API/Host -> Persistence -> Domain
Persistence -> GenericRepo -> Abstractions
Application -> Abstractions
```

Domain has no dependency on infrastructure. Application depends on contracts and use-case abstractions. Persistence implements storage details. API wires everything together.

## Request Flow

1. Client calls `POST /api/products/batch-update`.
2. API validates payload shape and batch limits.
3. Application checks tenant-scoped idempotency.
4. Application creates a `ProductUpdateBatch` and `ProductUpdateBatchItem` records.
5. Application publishes an integration event through the outbox.
6. Unit of Work commits batch rows and outbox row atomically.
7. API returns `202 Accepted` with `batchId`.
8. Outbox dispatcher publishes the event to RabbitMQ.
9. RabbitMQ consumer loads the batch and processes items in the background.
10. Products are upserted by `(TenantId, ItemId)`.
11. Batch status becomes `Completed`, `PartiallyFailed`, or `Failed`.

## Data Flow

```text
ProductUpdateBatch
  contains tenant, idempotency key, status, counters

ProductUpdateBatchItem
  contains tenant, item id, price, stock, metadata, status, error

Product
  unique per tenant and item: TenantId + ItemId

OutboxMessages
  stores integration events before RabbitMQ publish
```

## Tenant Isolation

The simplified implementation uses shared database and shared tables with `TenantId` on tenant-owned records.

Important rules:

- Products are unique by `(TenantId, ItemId)`.
- Batches are unique by `(TenantId, IdempotencyKey)` when an idempotency key is provided.
- Queries always include tenant filtering.
- One tenant batch failure updates only that tenant batch.

In production, `TenantId` should normally come from an authenticated token, API key, or tenant routing layer. The assessment payload includes `tenantId`, so the simplified version accepts it from the request.

## Failure Handling

- API validation failures return `400`.
- Duplicate idempotent submissions return the existing batch result.
- Outbox publish failures are retried with backoff.
- RabbitMQ consumer failures are retried or dead-lettered.
- Batch item failures are recorded without crashing the entire process.
- Failed items can be inspected through batch status endpoints.

## Retry Strategy

- Outbox dispatcher retries publishing pending messages.
- RabbitMQ dead-letter queue stores messages that cannot be processed.
- Product upsert logic is idempotent so retries are safe.
- Batch counters and item statuses make partial failure visible.

## Idempotency

Idempotency is tenant-scoped.

```text
TenantId + IdempotencyKey -> ProductUpdateBatch
```

This allows two tenants to use the same key independently while preventing duplicate processing for the same tenant request.

## Local Infrastructure

- PostgreSQL for application data and outbox.
- Redis for cache, locks, and idempotency support.
- RabbitMQ for background processing.
- Keycloak can be added for authenticated tenant identity and role-based permissions.
