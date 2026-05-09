using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Suppy.InventoryUpdate.Api.Domain.Tenancy;

namespace Suppy.InventoryUpdate.Api.Persistence.Sql;

internal static class ModelBuilderExtensions
{
    private const string IsDeletedProperty = "IsDeleted";
    private const string DeletedAtUtcProperty = "DeletedAtUtc";
    private const string CreatedAtUtcProperty = "CreatedAtUtc";
    private const string IdProperty = "Id";
    private const string TenantIdProperty = "TenantId";

    public static void ApplyTemplateConventions(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        ApplyTenantConventions(modelBuilder);
        ApplySoftDeleteFilters(modelBuilder);
        ApplyIndexes(modelBuilder);
    }

    private static void ApplyTenantConventions(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!CanApplyConventions(entityType) ||
                !HasProperty(entityType, TenantIdProperty, typeof(TenantId)))
            {
                continue;
            }

            var builder = modelBuilder.Entity(entityType.ClrType);

            builder.Property<TenantId>(TenantIdProperty)
                .HasConversion(
                    tenantId => tenantId.Value,
                    value => TenantId.From(value))
                .HasMaxLength(TenantId.MaxLength)
                .IsRequired();

            builder.HasIndex(TenantIdProperty);
        }
    }

    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!CanApplyConventions(entityType))
            {
                continue;
            }

            if (!HasProperty(entityType, IsDeletedProperty, typeof(bool)) ||
                !HasProperty(entityType, DeletedAtUtcProperty, typeof(DateTime?)))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var isDeleted = Expression.Call(
                typeof(EF),
                nameof(EF.Property),
                new[] { typeof(bool) },
                parameter,
                Expression.Constant(IsDeletedProperty));

            var body = Expression.Equal(isDeleted, Expression.Constant(false));
            var lambda = Expression.Lambda(body, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }

    private static void ApplyIndexes(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!CanApplyConventions(entityType))
            {
                continue;
            }

            var builder = modelBuilder.Entity(entityType.ClrType);

            if (HasProperty(entityType, IsDeletedProperty, typeof(bool)))
            {
                builder.HasIndex(IsDeletedProperty);
            }

            if (HasProperty(entityType, CreatedAtUtcProperty, typeof(DateTime), typeof(DateTime?)) &&
                HasProperty(entityType, IdProperty))
            {
                builder.HasIndex(CreatedAtUtcProperty, IdProperty);
            }
        }
    }

    private static bool CanApplyConventions(IReadOnlyEntityType entityType)
    {
        return entityType.ClrType.IsClass &&
               !entityType.IsOwned() &&
               entityType.BaseType is null;
    }

    private static bool HasProperty(IReadOnlyEntityType entityType, string propertyName, params Type[]? supportedTypes)
    {
        var property = entityType.FindProperty(propertyName);
        if (property is null)
        {
            return false;
        }

        return supportedTypes is null || supportedTypes.Length == 0 || supportedTypes.Contains(property.ClrType);
    }
}
