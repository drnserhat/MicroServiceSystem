using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MicroServiceSystem.Services.Identity.Domain.Aggregates;

namespace MicroServiceSystem.Services.Identity.Persistence.Configurations;

public sealed class PostgresClusterConfiguration : IEntityTypeConfiguration<PostgresCluster>
{
    public void Configure(EntityTypeBuilder<PostgresCluster> builder)
    {
        builder.ToTable("postgres_clusters");
        builder.HasKey(cluster => cluster.Id);

        builder.Property(cluster => cluster.Id).ValueGeneratedNever();
        builder.Property(cluster => cluster.Name).HasMaxLength(PostgresClusterConstraints.NameMaxLength).IsRequired();
        builder.Property(cluster => cluster.Slug).HasMaxLength(PostgresClusterConstraints.SlugMaxLength).IsRequired();
        builder.Property(cluster => cluster.Host).HasMaxLength(PostgresClusterConstraints.HostMaxLength).IsRequired();
        builder.Property(cluster => cluster.AdminSecretRef)
            .HasMaxLength(PostgresClusterConstraints.SecretRefMaxLength)
            .IsRequired();
        builder.HasIndex(cluster => cluster.Slug).IsUnique();
        builder.Ignore(cluster => cluster.DomainEvents);
    }
}

public sealed class TenantDatabaseBindingConfiguration : IEntityTypeConfiguration<TenantDatabaseBinding>
{
    public void Configure(EntityTypeBuilder<TenantDatabaseBinding> builder)
    {
        builder.ToTable("tenant_database_bindings");
        builder.HasKey(binding => binding.Id);

        builder.Property(binding => binding.Id).ValueGeneratedNever();
        builder.Property(binding => binding.ServiceKey)
            .HasMaxLength(TenantDatabaseBindingConstraints.ServiceKeyMaxLength)
            .IsRequired();
        builder.Property(binding => binding.DatabaseName)
            .HasMaxLength(TenantDatabaseBindingConstraints.DatabaseNameMaxLength)
            .IsRequired();
        builder.Property(binding => binding.Username)
            .HasMaxLength(TenantDatabaseBindingConstraints.UsernameMaxLength)
            .IsRequired();
        builder.Property(binding => binding.SecretRef)
            .HasMaxLength(TenantDatabaseBindingConstraints.SecretRefMaxLength)
            .IsRequired();
        builder.Property(binding => binding.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(binding => binding.SchemaVersion).HasMaxLength(64);
        builder.Property(binding => binding.LastError)
            .HasMaxLength(TenantDatabaseBindingConstraints.LastErrorMaxLength);

        builder.HasIndex(binding => new { binding.TenantId, binding.ServiceKey }).IsUnique();
        builder.HasIndex(binding => new { binding.ClusterId, binding.DatabaseName }).IsUnique();
        builder.Ignore(binding => binding.DomainEvents);
    }
}
