using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Suppy.InventoryUpdate.Api.Domain.Products.Entities;
using Suppy.InventoryUpdate.Api.Domain.Products.ValueObjects;
using Suppy.InventoryUpdate.Api.Domain.Tenancy;

namespace Suppy.InventoryUpdate.Api.Persistence.Products;

internal sealed class ProductEntityConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", table =>
        {
            table.HasCheckConstraint("CK_Products_Price_NonNegative", "\"Price\" >= 0");
            table.HasCheckConstraint("CK_Products_Stock_NonNegative", "\"Stock\" >= 0");
        });

        builder.HasKey(product => product.Id);

        builder.Property(product => product.Id)
            .ValueGeneratedNever();

        builder.Ignore(product => product.DomainEvents);

        builder.Property(product => product.TenantId)
            .HasConversion(
                tenantId => tenantId.Value,
                value => TenantId.From(value))
            .HasMaxLength(TenantId.MaxLength)
            .IsRequired();

        builder.Property(product => product.ItemId)
            .HasConversion(
                itemId => itemId.Value,
                value => ItemId.From(value))
            .HasMaxLength(ItemId.MaxLength)
            .IsRequired();

        builder.Property(product => product.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(product => product.Stock)
            .IsRequired();

        builder.Property(product => product.MetadataJson)
            .HasColumnType("text");

        builder.Property(product => product.LastBatchId)
            .IsRequired();

        builder.Property(product => product.LastUpdatedFromBatchAtUtc)
            .IsRequired();

        builder.HasIndex("TenantId", "ItemId")
            .IsUnique();

        builder.HasIndex("TenantId", "CreatedAtUtc", "Id");
        builder.HasIndex("TenantId", "LastUpdatedFromBatchAtUtc");
    }
}
