namespace Suppy.InventoryUpdate.Api.Domain.Entities;

public abstract class SoftDeletableEntity<TKey> : AuditableEntity<TKey> where TKey : notnull
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }

    public void MarkDeleted(DateTime utcNow)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAtUtc = utcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAtUtc = null;
    }
}
