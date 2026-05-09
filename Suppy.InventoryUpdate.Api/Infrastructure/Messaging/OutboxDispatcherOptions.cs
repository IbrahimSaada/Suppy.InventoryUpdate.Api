namespace Suppy.InventoryUpdate.Api.Infrastructure.Messaging;

internal sealed class OutboxDispatcherOptions
{
    public const string SectionName = "Messaging:Outbox";

    public bool Enabled { get; set; }

    public int BatchSize { get; set; } = 50;

    public int PollIntervalSeconds { get; set; } = 5;

    public int LockTimeoutSeconds { get; set; } = 30;

    public int MaxRetryCount { get; set; } = 10;

    public int BaseBackoffSeconds { get; set; } = 5;

    public int MaxErrorLength { get; set; } = 1024;
}
