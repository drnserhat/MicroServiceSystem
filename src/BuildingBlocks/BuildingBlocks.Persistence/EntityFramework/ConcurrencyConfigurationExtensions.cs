using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;

public static class ConcurrencyConfigurationExtensions
{
    /// <summary>
    /// Name of the shadow property that carries the row version. Only needed by code that has to read
    /// or set the token explicitly, such as tests.
    /// </summary>
    public const string ConcurrencyTokenName = "Version";

    /// <summary>
    /// Maps PostgreSQL's system <c>xmin</c> column as a concurrency token so a lost update fails loudly
    /// instead of overwriting a concurrent writer. This costs no schema change because every table
    /// already has <c>xmin</c>. Lives here so service persistence projects do not need their own
    /// dependency on the Npgsql provider.
    /// </summary>
    public static EntityTypeBuilder<TEntity> UseOptimisticConcurrency<TEntity>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Property<uint>(ConcurrencyTokenName)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        return builder;
    }
}
