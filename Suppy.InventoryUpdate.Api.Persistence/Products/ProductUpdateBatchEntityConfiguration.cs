using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Suppy.InventoryUpdate.Api.Domain.Products.Entities;
using Suppy.InventoryUpdate.Api.Domain.Products.Enums;
using Suppy.InventoryUpdate.Api.Domain.Products.ValueObjects;
using Suppy.InventoryUpdate.Api.Domain.Tenancy;

namespace Suppy.InventoryUpdate.Api.Persistence.Products;

internal sealed class ProductUpdateBatchEntityConfiguration : IEntityTypeConfiguration<ProductUpdateBatch>
{
    public void Configure(EntityTypeBuilder<ProductUpdateBatch> builder)
    {
        builder.ToTable("ProductUpdateBatches", table =>
        {
            table.HasCheckConstraint("CK_ProductUpdateBatches_TotalItems_Positive", "\"TotalItems\" > 0");
            table.HasCheckConstraint("CK_ProductUpdateBatches_ProcessedItems_NonNegative", "\"ProcessedItems\" >= 0");
            table.HasCheckConstraint("CK_ProductUpdateBatches_FailedItems_NonNegative", "\"FailedItems\" >= 0");
        });

        builder.HasKey(batch => batch.Id);

        builder.Property(batch => batch.Id)
            .ValueGeneratedNever();

        builder.Ignore(batch => batch.DomainEvents);

        builder.Property(batch => batch.TenantId)
            .HasConversion(
                tenantId => tenantId.Value,
                value => TenantId.From(value))
            .HasMaxLength(TenantId.MaxLength)
            .IsRequired();

        builder.Property(batch => batch.IdempotencyKey)
            .HasConversion(
                idempotencyKey => idempotencyKey == null ? null : idempotencyKey.Value,
                value => value == null ? null : BatchIdempotencyKey.From(value))
            .HasMaxLength(BatchIdempotencyKey.MaxLength);

        builder.Property(batch => batch.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(batch => batch.TotalItems)
            .IsRequired();

        builder.Property(batch => batch.ProcessedItems)
            .IsRequired();

        builder.Property(batch => batch.FailedItems)
            .IsRequired();

        builder.Property(batch => batch.ProcessingStartedAtUtc);
        builder.Property(batch => batch.CompletedAtUtc);

        builder.Property(batch => batch.FailureReason)
            .HasMaxLength(ProductUpdateBatchItem.MaxErrorMessageLength);

        builder.HasMany(batch => batch.Items)
            .WithOne()
            .HasForeignKey(item => item.BatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(batch => batch.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex("TenantId", "CreatedAtUtc", "Id");
        builder.HasIndex("TenantId", "Status", "CreatedAtUtc");

        builder.HasIndex("TenantId", "IdempotencyKey")
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL");
    }
}
