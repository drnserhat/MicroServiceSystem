using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using MicroServiceSystem.Services.Location.Domain.Aggregates;

namespace MicroServiceSystem.Services.Location.Persistence.Configurations;

public sealed class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("countries");
        builder.HasKey(country => country.Id);

        builder.UseOptimisticConcurrency();

        builder.Property(country => country.Code).HasMaxLength(3).IsRequired();
        builder.Property(country => country.Name).HasMaxLength(128).IsRequired();
        builder.HasIndex(country => new { country.TenantId, country.Code }).IsUnique();
        builder.Ignore(country => country.DomainEvents);
    }
}
