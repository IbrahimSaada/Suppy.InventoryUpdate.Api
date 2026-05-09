using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Suppy.InventoryUpdate.Api.Persistence.Outbox;

internal sealed class OutboxMessageEntityConfiguration : IEntityTypeConfiguration<OutboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<OutboxMessageEntity> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.MessageType)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.Headers)
            .HasColumnType("text");

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.LastError)
            .HasMaxLength(2048);

        builder.Property(x => x.LockId)
            .HasMaxLength(128);

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(128);

        builder.Property(x => x.CausationId)
            .HasMaxLength(128);

        builder.Property(x => x.IdempotencyKey)
            .HasMaxLength(256);

        builder.HasIndex(x => new { x.Status, x.AvailableAtUtc });
        builder.HasIndex(x => x.ProcessedAtUtc);
        builder.HasIndex(x => x.IdempotencyKey);
    }
}
