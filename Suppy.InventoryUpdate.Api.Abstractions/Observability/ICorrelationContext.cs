namespace Suppy.InventoryUpdate.Api.Abstractions.Observability;

public interface ICorrelationContext
{
    string? CorrelationId { get; }
}
