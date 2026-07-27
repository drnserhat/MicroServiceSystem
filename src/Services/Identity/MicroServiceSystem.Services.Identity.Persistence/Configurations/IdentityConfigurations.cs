using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using MicroServiceSystem.Services.Identity.Domain.Aggregates;

namespace MicroServiceSystem.Services.Identity.Persistence.Configurations;

public sealed class IdentityUserConfiguration : IEntityTypeConfiguration<IdentityUser>
{
    public void Configure(EntityTypeBuilder<IdentityUser> builder)
    {
        builder.ToTable("identity_users");
        builder.HasKey(user => user.Id);

        // Login, role assignment and disable all mutate the same row from different requests; without a
        // concurrency token the last writer silently wins and lockout counters get lost.
        builder.UseOptimisticConcurrency();

        builder.Property(user => user.Id).ValueGeneratedNever();
        builder.Property(user => user.Email).HasMaxLength(IdentityUserConstraints.EmailMaxLength).IsRequired();
        builder.Property(user => user.UserName).HasMaxLength(IdentityUserConstraints.UserNameMaxLength).IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(IdentityUserConstraints.PasswordHashMaxLength).IsRequired();
        builder.Property(user => user.PhoneNumber).HasMaxLength(32);
        builder.HasIndex(user => new { user.TenantId, user.Email }).IsUnique();
        builder.HasIndex(user => new { user.TenantId, user.UserName }).IsUnique();
        builder.Ignore(user => user.DomainEvents);
        builder.Property<List<Guid>>("_roleIds")
            .HasField("_roleIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("role_ids")
            .HasColumnType("uuid[]");
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(role => role.Id);
        builder.Property(role => role.Id).ValueGeneratedNever();
        builder.Property(role => role.Name).HasMaxLength(128).IsRequired();
        builder.Property(role => role.NormalizedName).HasMaxLength(128).IsRequired();
        builder.HasIndex(role => new { role.TenantId, role.NormalizedName }).IsUnique();
        builder.Ignore(role => role.DomainEvents);
        builder.Property<List<string>>("_permissions")
            .HasField("_permissions")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("permissions");
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(token => token.Id);

        // Makes refresh rotation single-winner: two requests racing on the same token both try to write
        // the revocation, and the loser fails instead of minting a second valid token family.
        builder.UseOptimisticConcurrency();

        builder.Property(token => token.Id).ValueGeneratedNever();
        builder.Property(token => token.TokenHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.Ignore(token => token.DomainEvents);
        builder.Ignore(token => token.IsActive);
    }
}
