namespace Suppy.InventoryUpdate.Api.Abstractions.Messaging;

public enum OutboxMessageStatus
{
    Pending = 0,
    Processing = 1,
    Succeeded = 2,
    Failed = 3,
    Poison = 4
}
