namespace Suppy.InventoryUpdate.Api.Infrastructure.Products;

internal sealed class ProductBatchProcessingOptions
{
    public const string SectionName = "ProductBatchProcessing";

    public bool Enabled { get; set; }

    public int BatchSize { get; set; } = 10;

    public int PollIntervalSeconds { get; set; } = 2;
}
