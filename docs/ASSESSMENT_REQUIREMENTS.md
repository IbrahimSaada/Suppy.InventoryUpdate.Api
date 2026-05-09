# Suppy Backend Technical Assessment Reference

Local source PDF:

```text
C:\Users\neneb\Downloads\Suppy_Backend_Technical_Assessment.pdf
```

The original PDF should not be committed to a public repository unless explicitly allowed by the company. This document keeps the implementation checklist in the project so development stays aligned with the assessment.

## Context

Suppy handles high-volume product and inventory updates from multiple tenants. Each tenant can send large batches of item updates multiple times per day.

The backend must handle these updates efficiently, reliably, and at scale.

## Input Shape

Each batch request contains:

```json
{
  "tenantId": "tenant_1",
  "items": [
    {
      "itemId": "123",
      "price": 100,
      "stock": 50,
      "metadata": {}
    }
  ]
}
```

Required item fields:

- `itemId`
- `price`
- `stock`

Optional item fields:

- `metadata`

## System Design Requirements

The design must cover:

- high load handling
- duplicate processing prevention
- data consistency
- multi-tenant support
- tenant isolation
- request flow
- data flow
- failure handling
- retry strategy
- idempotency approach

## Practical Implementation Requirements

Endpoint:

```http
POST /api/products/batch-update
```

Implementation must:

- validate input
- not process items synchronously inside the HTTP request
- use background processing
- store or update items in the database
- ensure idempotency
- keep code clean and maintainable
- explain structure and tradeoffs in the README

Expected successful response:

```http
202 Accepted
```

The response should include the accepted batch id and status.

## Bonus Technologies

Bonus points if used:

- RabbitMQ or another queue system
- Redis
- Docker
- cloud deployment
- logging and monitoring

Current bonus coverage:

- [x] RabbitMQ optional transport path for accepted batch integration events
- [x] Redis consumer idempotency when RabbitMQ mode is enabled
- [x] Docker Compose local stack
- [ ] Cloud deployment intentionally skipped
- [ ] Logging/monitoring intentionally skipped beyond default application logging

## Extra Production Improvements

Potential improvements:

- rate limiting
- retry policies
- dead-letter queue
- tenant isolation
- priority queues
- observability

## Implementation Plan Mapping

Current project decisions:

- shared database with shared tables
- tenant isolation through `TenantId`
- product uniqueness through `(TenantId, ItemId)`
- request idempotency through `(TenantId, IdempotencyKey)`
- API returns quickly with `202 Accepted`
- background processing handles product updates
- outbox and RabbitMQ can be enabled after the core flow works

## Acceptance Checklist

- [x] `POST /api/products/batch-update` exists
- [x] request validation rejects invalid tenant, empty items, invalid price, invalid stock
- [x] endpoint returns `202 Accepted`, not synchronous processing result
- [x] batch record is stored
- [x] batch items are stored
- [x] products are created or updated in DB by `(TenantId, ItemId)`
- [x] duplicate request with same tenant idempotency key does not create duplicate processing
- [x] one tenant cannot affect another tenant's product rows
- [x] tenant-aware rate limiting prevents one tenant from exhausting product API capacity
- [x] background processing updates batch status
- [x] failures are recorded and retryable
- [x] README explains architecture, flow, failures, retries, idempotency, and tradeoffs
- [x] Docker/local infrastructure instructions are clear
