using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MicroServiceSystem.Services.Identity.Domain.Aggregates;

namespace MicroServiceSystem.Services.Identity.Persistence.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");
        builder.HasKey(tenant => tenant.Id);

        builder.Property(tenant => tenant.Id).ValueGeneratedNever();
        builder.Property(tenant => tenant.Name).HasMaxLength(TenantConstraints.NameMaxLength).IsRequired();
        builder.Property(tenant => tenant.Slug).HasMaxLength(TenantConstraints.SlugMaxLength).IsRequired();
        builder.HasIndex(tenant => tenant.Slug).IsUnique();
        builder.Ignore(tenant => tenant.DomainEvents);
    }
}
