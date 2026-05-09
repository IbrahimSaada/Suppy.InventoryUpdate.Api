using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Suppy.InventoryUpdate.Api.Domain.Products.Entities;
using Suppy.InventoryUpdate.Api.Domain.Products.ValueObjects;
using Suppy.InventoryUpdate.Api.Domain.Tenancy;

namespace Suppy.InventoryUpdate.Api.Persistence.Products;

internal sealed class ProductUpdateBatchItemEntityConfiguration : IEntityTypeConfiguration<ProductUpdateBatchItem>
{
    public void Configure(EntityTypeBuilder<ProductUpdateBatchItem> builder)
    {
        builder.ToTable("ProductUpdateBatchItems", table =>
        {
            table.HasCheckConstraint("CK_ProductUpdateBatchItems_Price_NonNegative", "\"Price\" >= 0");
            table.HasCheckConstraint("CK_ProductUpdateBatchItems_Stock_NonNegative", "\"Stock\" >= 0");
        });

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id)
            .ValueGeneratedNever();

        builder.Property(item => item.TenantId)
            .HasConversion(
                tenantId => tenantId.Value,
                value => TenantId.From(value))
            .HasMaxLength(TenantId.MaxLength)
            .IsRequired();

        builder.Property(item => item.BatchId)
            .IsRequired();

        builder.Property(item => item.ItemId)
            .HasConversion(
                itemId => itemId.Value,
                value => ItemId.From(value))
            .HasMaxLength(ItemId.MaxLength)
            .IsRequired();

        builder.Property(item => item.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(item => item.Stock)
            .IsRequired();

        builder.Property(item => item.MetadataJson)
            .HasColumnType("text");

        builder.Property(item => item.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(item => item.ErrorMessage)
            .HasMaxLength(ProductUpdateBatchItem.MaxErrorMessageLength);

        builder.Property(item => item.ProcessingStartedAtUtc);
        builder.Property(item => item.ProcessedAtUtc);
        builder.Property(item => item.FailedAtUtc);

        builder.HasIndex("BatchId", "ItemId")
            .IsUnique();

        builder.HasIndex("TenantId", "BatchId");
        builder.HasIndex("TenantId", "ItemId");
        builder.HasIndex("TenantId", "Status");
    }
}
