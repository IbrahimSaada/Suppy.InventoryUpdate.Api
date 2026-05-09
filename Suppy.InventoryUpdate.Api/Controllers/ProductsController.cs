using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Suppy.InventoryUpdate.Api.Application.Dispatching;
using Suppy.InventoryUpdate.Api.Application.Features.Products.GetBatchStatus;
using Suppy.InventoryUpdate.Api.Application.Features.Products.ListProducts;
using Suppy.InventoryUpdate.Api.Application.Features.Products.RetryBatchUpdate;
using Suppy.InventoryUpdate.Api.Application.Features.Products.SubmitBatchUpdate;
using Suppy.InventoryUpdate.Api.Infrastructure.RateLimiting;
using Suppy.InventoryUpdate.Api.Presentation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Suppy.InventoryUpdate.Api.Controllers;

[ApiController]
[Route("api/products")]
[EnableRateLimiting(TenantRateLimitingPolicyNames.Tenant)]
public sealed class ProductsController : ControllerBase
{
    private readonly IRequestDispatcher _dispatcher;

    public ProductsController(IRequestDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpPost("batch-update")]
    [ProducesResponseType(typeof(ApiEnvelope<SubmitProductBatchUpdateResult>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiEnvelope<ApiErrorPayload>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitBatchUpdate(
        [FromBody] ProductBatchUpdateRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKeyHeader,
        CancellationToken ct)
    {
        var command = new SubmitProductBatchUpdateCommand(
            request.TenantId,
            MapItems(request.Items),
            ResolveIdempotencyKey(idempotencyKeyHeader, request.IdempotencyKey, request));

        var result = await _dispatcher.Send(command, ct);
        if (result.IsFailure)
        {
            return this.ToActionResult(result);
        }

        var message = result.Value.WasDuplicate
            ? "Product batch was already accepted."
            : "Product batch accepted for background processing.";

        return Accepted(ApiEnvelope<SubmitProductBatchUpdateResult>.Success(result.Value, message));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiEnvelope<ListProductsResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<ApiErrorPayload>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListProducts(
        [FromQuery] string tenantId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _dispatcher.Send(new ListProductsQuery(tenantId, page, pageSize), ct);
        return this.ToActionResult(result, "Products fetched.");
    }

    [HttpGet("batches/{batchId:guid}")]
    [ProducesResponseType(typeof(ApiEnvelope<ProductBatchStatusResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<ApiErrorPayload>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBatchStatus(Guid batchId, CancellationToken ct)
    {
        var result = await _dispatcher.Send(new GetProductBatchStatusQuery(batchId), ct);
        return this.ToActionResult(result, "Product batch status fetched.");
    }

    [HttpPost("batches/{batchId:guid}/retry")]
    [ProducesResponseType(typeof(ApiEnvelope<RetryProductBatchUpdateResult>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiEnvelope<ApiErrorPayload>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiEnvelope<ApiErrorPayload>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RetryBatch(Guid batchId, CancellationToken ct)
    {
        var result = await _dispatcher.Send(new RetryProductBatchUpdateCommand(batchId), ct);
        if (result.IsFailure)
        {
            return this.ToActionResult(result);
        }

        return Accepted(ApiEnvelope<RetryProductBatchUpdateResult>.Success(
            result.Value,
            "Product batch retry accepted for background processing."));
    }

    private static IReadOnlyCollection<SubmitProductBatchUpdateItem> MapItems(
        IReadOnlyCollection<ProductBatchUpdateItemRequest>? items)
    {
        if (items is null || items.Count == 0)
        {
            return Array.Empty<SubmitProductBatchUpdateItem>();
        }

        return items
            .Select(item => new SubmitProductBatchUpdateItem(
                item.ItemId,
                item.Price,
                item.Stock,
                SerializeMetadata(item.Metadata)))
            .ToArray();
    }

    private static string ResolveIdempotencyKey(
        string? headerValue,
        string? bodyValue,
        ProductBatchUpdateRequest request)
    {
        if (!string.IsNullOrWhiteSpace(headerValue))
        {
            return headerValue.Trim();
        }

        if (!string.IsNullOrWhiteSpace(bodyValue))
        {
            return bodyValue.Trim();
        }

        // The assessment sample does not include an idempotency key, so we create
        // a deterministic fallback from the normalized payload for demo safety.
        return $"auto:{ComputePayloadHash(request)}";
    }

    private static string ComputePayloadHash(ProductBatchUpdateRequest request)
    {
        var canonicalItems = request.Items?
            .Select(item => new
            {
                itemId = item.ItemId?.Trim(),
                item.Price,
                item.Stock,
                metadata = SerializeMetadata(item.Metadata)
            })
            .Cast<object>()
            .ToArray() ?? Array.Empty<object>();

        var canonicalPayload = JsonSerializer.Serialize(new
        {
            tenantId = request.TenantId?.Trim().ToLowerInvariant(),
            items = canonicalItems
        });

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalPayload));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string? SerializeMetadata(JsonElement? metadata)
    {
        if (metadata is null ||
            metadata.Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return null;
        }

        return metadata.Value.GetRawText();
    }
}

public sealed record ProductBatchUpdateRequest(
    string TenantId,
    IReadOnlyCollection<ProductBatchUpdateItemRequest>? Items,
    string? IdempotencyKey = null);

public sealed record ProductBatchUpdateItemRequest(
    string ItemId,
    decimal Price,
    int Stock,
    JsonElement? Metadata = null);
