using Suppy.InventoryUpdate.Api.Abstractions.Products;
using Suppy.InventoryUpdate.Api.Application.Dispatching;
using Suppy.InventoryUpdate.Api.Application.Features.Products.ProcessBatchUpdate;
using Microsoft.Extensions.Options;

namespace Suppy.InventoryUpdate.Api.Infrastructure.Products;

internal sealed class ProductBatchProcessingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProductBatchProcessingWorker> _logger;
    private readonly ProductBatchProcessingOptions _options;

    public ProductBatchProcessingWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<ProductBatchProcessingOptions> options,
        ILogger<ProductBatchProcessingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Product batch processing worker is disabled.");
            return;
        }

        _logger.LogInformation(
            "Product batch processing worker started with batch size {BatchSize}.",
            _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingBatchesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Product batch processing worker failed during polling cycle.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessPendingBatchesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();

        var processingStore = scope.ServiceProvider.GetRequiredService<IProductBatchProcessingStore>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IRequestDispatcher>();

        var batchIds = await processingStore.ListAcceptedBatchIdsAsync(_options.BatchSize, ct);
        if (batchIds.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Product batch worker found {BatchCount} accepted batches.", batchIds.Count);

        foreach (var batchId in batchIds)
        {
            var result = await dispatcher.Send(new ProcessProductBatchUpdateCommand(batchId), ct);
            if (result.IsFailure)
            {
                _logger.LogWarning(
                    "Product batch {BatchId} failed to process: {ErrorCode} {ErrorMessage}",
                    batchId,
                    result.Error.Code,
                    result.Error.Message);
            }
        }
    }
}
