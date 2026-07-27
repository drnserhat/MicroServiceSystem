using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroServiceSystem.BuildingBlocks.Persistence.Inbox;

public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public const string TableName = "inbox_messages";

    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(TableName);

        builder.HasKey(message => message.MessageId);

        builder.Property(message => message.MessageId).ValueGeneratedNever();

        builder.Property(message => message.EventName).HasMaxLength(256).IsRequired();

        builder.Property(message => message.Error).HasMaxLength(4000);

        builder.HasIndex(message => message.ProcessedOnUtc).HasDatabaseName("ix_inbox_messages_processed");

        builder.HasIndex(message => message.LockedUntilUtc).HasDatabaseName("ix_inbox_messages_locked_until");
    }
}
