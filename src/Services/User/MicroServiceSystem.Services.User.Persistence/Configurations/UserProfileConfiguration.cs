using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using MicroServiceSystem.Services.User.Domain.Aggregates;

namespace MicroServiceSystem.Services.User.Persistence.Configurations;

public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");
        builder.HasKey(profile => profile.Id);

        // Profile updates and the deactivation path can arrive concurrently from HTTP and consumers.
        builder.UseOptimisticConcurrency();

        builder.Property(profile => profile.Id).ValueGeneratedNever();
        builder.Property(profile => profile.FirstName).HasMaxLength(UserProfileConstraints.NameMaxLength).IsRequired();
        builder.Property(profile => profile.LastName).HasMaxLength(UserProfileConstraints.NameMaxLength).IsRequired();
        builder.Property(profile => profile.DisplayName).HasMaxLength(UserProfileConstraints.DisplayNameMaxLength).IsRequired();
        builder.HasIndex(profile => new { profile.TenantId, profile.Id }).IsUnique();
        builder.Ignore(profile => profile.DomainEvents);
    }
}
