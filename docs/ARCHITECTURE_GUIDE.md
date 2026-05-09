# Suppy Inventory Update Architecture

## Assessment Problem

Multiple tenants send high-volume product updates. Each update contains an `itemId`, `price`, `stock`, and optional metadata. The backend must accept large batches without blocking the API request, avoid duplicate processing, keep product data consistent, and isolate tenants from each other.

The implementation checklist is maintained in `docs/ASSESSMENT_REQUIREMENTS.md`.

## Proposed Architecture

```text
Tenant client
  -> API endpoint
  -> tenant rate limit
  -> validation
  -> tenant-scoped idempotency
  -> batch persistence
  -> outbox persistence
  -> 202 Accepted
  -> DB-backed background worker
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
2. Tenant rate limiting checks the tenant partition.
3. API validates payload shape and batch limits.
4. Application checks tenant-scoped idempotency.
5. Application creates a `ProductUpdateBatch` and `ProductUpdateBatchItem` records.
6. Application publishes an integration event through the outbox.
7. Unit of Work commits batch rows and outbox row atomically.
8. API returns `202 Accepted` with `batchId`.
9. The DB-backed background worker polls accepted batch ids.
10. The worker dispatches `ProcessProductBatchUpdateCommand` for each accepted batch.
11. Products are upserted by `(TenantId, ItemId)`.
12. Batch status becomes `Completed`, `PartiallyFailed`, or `Failed`.

RabbitMQ remains available as an optional transport for integration events. The core assessment flow does not require the HTTP request to wait on RabbitMQ.

## Processing Modes

The project supports two asynchronous processing modes.

Default mode:

```text
POST request
  -> batch rows
  -> DB-backed background worker
  -> product upsert
```

Bonus RabbitMQ mode:

```text
POST request
  -> batch rows + outbox row
  -> outbox dispatcher
  -> RabbitMQ exchange
  -> RabbitMQ consumer
  -> ProcessProductBatchUpdateCommand
  -> product upsert
```

RabbitMQ mode should be paired with Redis enabled. Redis is used by the consumer idempotency store so duplicate RabbitMQ deliveries do not reprocess the same message.

## Application Use Case

The first use case is `SubmitProductBatchUpdateCommand`.

It is intentionally limited to acceptance work:

- validate tenant id, item count, item id, price, stock, and metadata size
- normalize tenant and item identifiers through domain value objects
- check existing batch by `(TenantId, IdempotencyKey)` when an idempotency key is provided
- create `ProductUpdateBatch` and child `ProductUpdateBatchItem` rows
- publish `ProductBatchUpdateAcceptedIntegrationEvent`
- return the accepted `batchId` and status

It does not update `Product` rows directly. Product updates are handled by the background processor after the batch has been accepted.

`ProductBatchUpdateAcceptedIntegrationEventConsumer` handles the RabbitMQ bonus path. It consumes `ProductBatchUpdateAcceptedIntegrationEvent` and dispatches `ProcessProductBatchUpdateCommand`, keeping RabbitMQ transport code outside the domain model.

The processing use case is `ProcessProductBatchUpdateCommand`.

It runs outside the HTTP request:

- claims only `Accepted` batches before processing
- loads batch items
- upserts `Product` rows by `(TenantId, ItemId)`
- marks each item as `Processed` or `Failed`
- updates batch counters and final status

The retry use case is `RetryProductBatchUpdateCommand`.

It keeps retry behavior explicit:

- only `Failed` and `PartiallyFailed` batches can be retried
- only failed items are reset to `Pending`
- already processed items stay processed
- the batch status returns to `Accepted`
- the existing background worker processes the retry asynchronously

The read use case is `ListProductsQuery`.

It is intentionally read-only:

- accepts `tenantId`, `page`, and `pageSize`
- validates tenant and pagination limits
- reads only that tenant's product rows
- returns pagination metadata so high-volume tenants can be browsed safely

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

## Domain Model

The domain starts with three tenant-scoped concepts:

- `Product` stores the current product state for one tenant and one external item id.
- `ProductUpdateBatch` stores the accepted request and tracks asynchronous processing progress.
- `ProductUpdateBatchItem` stores each requested product update and its individual processing result.

Value objects keep boundary data consistent:

- `TenantId` normalizes and validates tenant identity.
- `ItemId` validates the external item id from the tenant.
- `BatchIdempotencyKey` stores optional request idempotency safely.

Batch status is explicit:

```text
Accepted -> Processing -> Completed
Accepted -> Processing -> PartiallyFailed
Accepted -> Processing -> Failed
```

Each item is also tracked independently:

```text
Pending -> Processing -> Processed
Pending -> Processing -> Failed
```

This lets one bad item fail without losing the full batch history.

## Tenant Isolation

The simplified implementation uses shared database and shared tables with `TenantId` on tenant-owned records.

Important rules:

- Products are unique by `(TenantId, ItemId)`.
- Batches are unique by `(TenantId, IdempotencyKey)` when an idempotency key is provided.
- Queries always include tenant filtering.
- One tenant batch failure updates only that tenant batch.
- Rate limiting is partitioned by tenant when tenant identity is available.

In production, `TenantId` should normally come from an authenticated token, API key, or tenant routing layer. The assessment payload includes `tenantId`, so the simplified version accepts it from the request.

## Tenant Rate Limiting

Product endpoints use a tenant-aware fixed-window rate limiter.

Partition resolution order:

```text
X-Tenant-Id header
tenant_id JWT claim
tenantId query string
remote IP fallback
```

This prevents one tenant from consuming all product API capacity for other tenants. For the batch endpoint, clients should send `X-Tenant-Id` because infrastructure should rate-limit before reading and validating large request bodies. The body `tenantId` is still validated by the application layer and stored on tenant-owned rows.

Current default:

```text
120 requests / 60 seconds / tenant partition
```

When the limit is exceeded, the API returns:

```http
429 Too Many Requests
```

## Tenant Foundation

Tenant support is implemented as a first-class foundation:

- `TenantId` is a domain value object.
- Tenant IDs are trimmed and normalized to lowercase.
- Tenant IDs are limited to 100 characters.
- Allowed characters are lowercase letters, numbers, dot, dash, and underscore.
- Tenant IDs must start with a letter or number.
- Tenant-owned domain entities should implement `ITenantScoped`.
- Tenant-owned aggregate roots can inherit from `TenantScopedAggregateRoot`.
- Tenant-owned child entities can inherit from `TenantScopedEntity`.

The API also exposes `ICurrentTenant` for future header/JWT-based tenant resolution:

```text
X-Tenant-Id header -> preferred local/dev source
tenant_id claim   -> preferred production source after authentication
```

For this assessment endpoint, the request body still includes `tenantId`. The command handler should convert it to `TenantId` and store it on every tenant-owned row.

Persistence applies a convention for tenant-scoped entities:

```text
TenantId value object -> required string column
TenantId column       -> indexed
```

Feature-specific mappings still own business indexes, for example:

```text
Products: unique (TenantId, ItemId)
Batches:  unique (TenantId, IdempotencyKey)
```

Product read API:

```http
GET /api/products?tenantId=tenant_1&page=1&pageSize=50
```

Retry API:

```http
POST /api/products/batches/{batchId}/retry
```

## Failure Handling

- API validation failures return `400`.
- Duplicate idempotent submissions return the existing batch result.
- Outbox publish failures are retried with backoff.
- Background worker failures are logged and retried on the next polling cycle when the transaction rolls back.
- RabbitMQ consumer failures are retried or dead-lettered when RabbitMQ is enabled.
- Item-level failures are stored on `ProductUpdateBatchItem.ErrorMessage`.
- Failed or partially failed batches expose `canRetry = true` from the batch status endpoint.
- Batch item failures are recorded without crashing the entire process.
- Failed items can be inspected through batch status endpoints.

## Retry Strategy

- Background worker polling retries accepted batches that were not successfully committed.
- Manual retry resets failed items and makes the batch accepted again.
- Outbox dispatcher retries publishing pending integration messages.
- RabbitMQ dead-letter queue stores messages that cannot be processed when RabbitMQ is enabled.
- Product upsert logic is idempotent so retries are safe.
- Batch counters and item statuses make partial failure visible.

## Idempotency

Idempotency is tenant-scoped.

```text
TenantId + IdempotencyKey -> ProductUpdateBatch
```

This allows two tenants to use the same key independently while preventing duplicate processing for the same tenant request.

Clients should send the key through the `Idempotency-Key` header. The API also accepts a body `idempotencyKey` field. If neither is provided, the controller creates a deterministic `auto:<sha256>` key from the normalized payload so the assessment sample remains idempotent without adding extra required fields.

## Local Infrastructure

- PostgreSQL for application data and outbox.
- Redis for cache, locks, and idempotency support.
- RabbitMQ for optional integration-event transport.
- Keycloak can be added for authenticated tenant identity and role-based permissions.

Full local setup instructions are in `docs/LOCAL_INFRASTRUCTURE.md`.
