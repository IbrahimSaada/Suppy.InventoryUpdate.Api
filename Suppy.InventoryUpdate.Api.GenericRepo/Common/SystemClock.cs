using Suppy.InventoryUpdate.Api.Abstractions.Clock;

namespace Suppy.InventoryUpdate.Api.GenericRepo.Common;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
