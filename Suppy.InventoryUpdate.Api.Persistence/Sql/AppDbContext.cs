using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Suppy.InventoryUpdate.Api.Persistence.Outbox;

namespace Suppy.InventoryUpdate.Api.Persistence.Sql;

public sealed class AppDbContext : DbContext
{
    private const string CreatedAtUtcProperty = "CreatedAtUtc";
    private const string UpdatedAtUtcProperty = "UpdatedAtUtc";
    private const string IsDeletedProperty = "IsDeleted";
    private const string DeletedAtUtcProperty = "DeletedAtUtc";

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    internal DbSet<OutboxMessageEntity> OutboxMessages => Set<OutboxMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        modelBuilder.ApplyTemplateConventions();

        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges()
    {
        ApplyAuditAndSoftDeleteRules();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        ApplyAuditAndSoftDeleteRules();
        return base.SaveChangesAsync(ct);
    }

    private void ApplyAuditAndSoftDeleteRules()
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                SetCreatedAt(entry, utcNow);
                continue;
            }

            if (entry.State == EntityState.Modified)
            {
                SetUpdatedAt(entry, utcNow);
                continue;
            }

            if (entry.State == EntityState.Deleted && SupportsSoftDelete(entry))
            {
                entry.State = EntityState.Modified;
                SetIsDeleted(entry, true);
                SetDeletedAt(entry, utcNow);
                SetUpdatedAt(entry, utcNow);
            }
        }
    }

    private static void SetCreatedAt(EntityEntry entry, DateTime utcNow)
    {
        var property = entry.PropertyOrNull(CreatedAtUtcProperty);
        if (property is null)
        {
            return;
        }

        if (property.Metadata.ClrType == typeof(DateTime))
        {
            var current = property.CurrentValue is DateTime dateTime ? dateTime : default;
            if (current == default)
            {
                property.CurrentValue = utcNow;
            }

            return;
        }

        if (property.Metadata.ClrType == typeof(DateTime?))
        {
            var current = property.CurrentValue as DateTime?;
            if (!current.HasValue || current.Value == default)
            {
                property.CurrentValue = utcNow;
            }
        }
    }

    private static void SetUpdatedAt(EntityEntry entry, DateTime utcNow)
    {
        var property = entry.PropertyOrNull(UpdatedAtUtcProperty);
        if (property is null)
        {
            return;
        }

        if (property.Metadata.ClrType == typeof(DateTime) || property.Metadata.ClrType == typeof(DateTime?))
        {
            property.CurrentValue = utcNow;
        }
    }

    private static bool SupportsSoftDelete(EntityEntry entry)
    {
        var isDeletedProperty = entry.PropertyOrNull(IsDeletedProperty);
        var deletedAtProperty = entry.PropertyOrNull(DeletedAtUtcProperty);

        return isDeletedProperty?.Metadata.ClrType == typeof(bool) &&
               deletedAtProperty?.Metadata.ClrType == typeof(DateTime?);
    }

    private static void SetIsDeleted(EntityEntry entry, bool value)
    {
        var property = entry.PropertyOrNull(IsDeletedProperty);
        if (property is not null && property.Metadata.ClrType == typeof(bool))
        {
            property.CurrentValue = value;
        }
    }

    private static void SetDeletedAt(EntityEntry entry, DateTime utcNow)
    {
        var property = entry.PropertyOrNull(DeletedAtUtcProperty);
        if (property is not null && property.Metadata.ClrType == typeof(DateTime?))
        {
            property.CurrentValue = utcNow;
        }
    }
}

internal static class EntityEntryExtensions
{
    public static PropertyEntry? PropertyOrNull(this EntityEntry entry, string propertyName)
    {
        var property = entry.Metadata.FindProperty(propertyName);
        return property is null ? null : entry.Property(propertyName);
    }
}

