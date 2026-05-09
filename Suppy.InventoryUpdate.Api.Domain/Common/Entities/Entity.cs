using System.Collections.Generic;

namespace Suppy.InventoryUpdate.Api.Domain.Entities;

public abstract class Entity<TKey> where TKey : notnull
{
    public virtual TKey Id { get; protected set; } = default!;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TKey> other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        if (IsTransient() || other.IsTransient())
        {
            return false;
        }

        return EqualityComparer<TKey>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode()
    {
        if (IsTransient())
        {
            return base.GetHashCode();
        }

        return HashCode.Combine(GetType(), Id);
    }

    public static bool operator ==(Entity<TKey>? left, Entity<TKey>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Entity<TKey>? left, Entity<TKey>? right)
    {
        return !Equals(left, right);
    }

    private bool IsTransient()
    {
        return EqualityComparer<TKey>.Default.Equals(Id, default!);
    }
}
