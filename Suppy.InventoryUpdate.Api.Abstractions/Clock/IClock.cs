namespace Suppy.InventoryUpdate.Api.Abstractions.Clock;

public interface IClock
{
    DateTime UtcNow { get; }
}
