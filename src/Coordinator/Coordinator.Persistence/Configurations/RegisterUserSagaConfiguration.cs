using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Coordinator.Domain.Aggregates;
using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;

namespace Coordinator.Persistence.Configurations;

public sealed class RegisterUserSagaConfiguration : IEntityTypeConfiguration<RegisterUserSaga>
{
    public void Configure(EntityTypeBuilder<RegisterUserSaga> builder)
    {
        builder.ToTable("register_user_sagas");
        builder.HasKey(saga => saga.Id);

        // Stops the recovery service and the in-flight saga from advancing the same instance in parallel:
        // whichever checkpoint lands second is rejected instead of overwriting the other's state.
        builder.UseOptimisticConcurrency();

        builder.Property(saga => saga.Id).ValueGeneratedNever();
        builder.Property(saga => saga.Email).HasMaxLength(256).IsRequired();
        builder.Property(saga => saga.UserName).HasMaxLength(128).IsRequired();
        builder.Property(saga => saga.DisplayName).HasMaxLength(256).IsRequired();
        builder.Property(saga => saga.FailureReason).HasMaxLength(1024);
        builder.Property(saga => saga.State).HasConversion<string>().HasMaxLength(64);
        builder.Property(saga => saga.LockedBy).HasMaxLength(128);
        builder.Ignore(saga => saga.DomainEvents);

        // Recovery scans for non-terminal sagas whose lease lapsed; this is the predicate it filters on.
        builder.HasIndex(saga => new { saga.State, saga.LockedUntilUtc })
            .HasDatabaseName("ix_register_user_sagas_state_locked_until");
    }
}
