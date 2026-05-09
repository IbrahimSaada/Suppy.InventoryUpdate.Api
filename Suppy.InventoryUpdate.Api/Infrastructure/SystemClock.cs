using Suppy.InventoryUpdate.Api.Abstractions.Clock;

namespace Suppy.InventoryUpdate.Api.Infrastructure;

internal sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
