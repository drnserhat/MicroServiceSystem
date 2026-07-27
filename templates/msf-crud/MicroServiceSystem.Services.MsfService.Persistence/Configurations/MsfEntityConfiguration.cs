using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MicroServiceSystem.Services.MsfService.Domain.Aggregates;
using MsfEntityAggregate = MicroServiceSystem.Services.MsfService.Domain.Aggregates.MsfEntity;

namespace MicroServiceSystem.Services.MsfService.Persistence.Configurations;

internal sealed class MsfEntityConfiguration : IEntityTypeConfiguration<MsfEntityAggregate>
{
    public void Configure(EntityTypeBuilder<MsfEntityAggregate> builder)
    {
        builder.ToTable("msf_entities");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Name)
            .HasMaxLength(MsfEntityConstraints.NameMaxLength)
            .IsRequired();

        builder.Property(entity => entity.Description)
            .HasMaxLength(MsfEntityConstraints.DescriptionMaxLength);

        builder.HasIndex(entity => new { entity.TenantId, entity.Name })
            .IsUnique()
            .HasFilter("is_deleted = false");
    }
}
