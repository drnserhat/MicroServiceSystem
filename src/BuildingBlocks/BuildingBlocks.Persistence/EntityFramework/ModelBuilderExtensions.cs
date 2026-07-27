using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MicroServiceSystem.BuildingBlocks.Persistence.Inbox;
using MicroServiceSystem.BuildingBlocks.Persistence.Outbox;

namespace MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;

public static class ModelBuilderExtensions
{
    /// <summary>
    /// Adds the outbox and inbox tables to a service database. Only services that publish or consume
    /// integration events need to call this.
    /// </summary>
    public static ModelBuilder ApplyMessagingStore(
        this ModelBuilder modelBuilder,
        bool includeOutbox = true,
        bool includeInbox = true)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        if (includeOutbox)
        {
            modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        }

        if (includeInbox)
        {
            modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
        }

        return modelBuilder;
    }

    /// <summary>
    /// Rewrites table, column, key and index names to snake_case, which is the PostgreSQL convention
    /// and avoids quoted identifiers in every hand written Dapper query.
    /// </summary>
    public static ModelBuilder UseSnakeCaseNames(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.GetTableName() is { } tableName)
            {
                entityType.SetTableName(ToSnakeCase(tableName));
            }

            foreach (IMutableProperty property in entityType.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.GetColumnName()));
            }

            foreach (IMutableKey key in entityType.GetKeys())
            {
                key.SetName(ToSnakeCase(key.GetName()!));
            }

            foreach (IMutableForeignKey foreignKey in entityType.GetForeignKeys())
            {
                foreignKey.SetConstraintName(ToSnakeCase(foreignKey.GetConstraintName()!));
            }

            foreach (IMutableIndex index in entityType.GetIndexes())
            {
                index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName()!));
            }
        }

        return modelBuilder;
    }

    private static string ToSnakeCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var builder = new StringBuilder(value.Length + 8);

        for (int index = 0; index < value.Length; index++)
        {
            char current = value[index];

            if (char.IsUpper(current))
            {
                bool previousIsLower = index > 0 && char.IsLower(value[index - 1]);
                bool nextIsLower = index + 1 < value.Length && char.IsLower(value[index + 1]);

                if (index > 0 && value[index - 1] != '_' && (previousIsLower || nextIsLower))
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(current));
                continue;
            }

            builder.Append(current);
        }

        return builder.ToString();
    }
}
