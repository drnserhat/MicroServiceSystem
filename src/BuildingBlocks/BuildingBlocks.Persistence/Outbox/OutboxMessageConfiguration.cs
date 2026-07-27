using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroServiceSystem.BuildingBlocks.Persistence.Outbox;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public const string TableName = "outbox_messages";

    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(TableName);

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id).ValueGeneratedNever();

        builder.Property(message => message.EventName).HasMaxLength(256).IsRequired();

        builder.Property(message => message.Payload).IsRequired();

        builder.Property(message => message.CorrelationId).HasMaxLength(128);

        builder.Property(message => message.TraceParent).HasMaxLength(128);

        builder.Property(message => message.Source).HasMaxLength(128);

        builder.Property(message => message.Error).HasMaxLength(4000);

        builder.Property(message => message.LockedBy).HasMaxLength(128);

        // The relay always scans unprocessed rows in arrival order; this index serves exactly that scan.
        builder.HasIndex(message => new { message.ProcessedOnUtc, message.OccurredOnUtc })
            .HasDatabaseName("ix_outbox_messages_unprocessed");

        // Health checks count open poison rows; an index keeps that cheap even when the table is large.
        builder.HasIndex(message => message.DeadLetteredOnUtc)
            .HasDatabaseName("ix_outbox_messages_dead_lettered");
    }
}
