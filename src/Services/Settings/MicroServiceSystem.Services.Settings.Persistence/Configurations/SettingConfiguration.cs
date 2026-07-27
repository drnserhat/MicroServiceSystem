using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using MicroServiceSystem.Services.Settings.Domain.Aggregates;

namespace MicroServiceSystem.Services.Settings.Persistence.Configurations;

public sealed class SettingConfiguration : IEntityTypeConfiguration<Setting>
{
    public void Configure(EntityTypeBuilder<Setting> builder)
    {
        builder.ToTable("settings");
        builder.HasKey(setting => setting.Id);

        builder.UseOptimisticConcurrency();

        builder.Property(setting => setting.Key).HasMaxLength(128).IsRequired();
        builder.Property(setting => setting.Value).IsRequired();
        builder.HasIndex(setting => new { setting.TenantId, setting.Key }).IsUnique();
        builder.Ignore(setting => setting.DomainEvents);
    }
}
