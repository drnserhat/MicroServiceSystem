using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Coordinator.Domain.Aggregates;

namespace Coordinator.Persistence.Configurations;

public sealed class RegisterUserSagaConfiguration : IEntityTypeConfiguration<RegisterUserSaga>
{
    public void Configure(EntityTypeBuilder<RegisterUserSaga> builder)
    {
        builder.ToTable("register_user_sagas");
        builder.HasKey(saga => saga.Id);
        builder.Property(saga => saga.Id).ValueGeneratedNever();
        builder.Property(saga => saga.Email).HasMaxLength(256).IsRequired();
        builder.Property(saga => saga.UserName).HasMaxLength(128).IsRequired();
        builder.Property(saga => saga.DisplayName).HasMaxLength(256).IsRequired();
        builder.Property(saga => saga.FailureReason).HasMaxLength(1024);
        builder.Property(saga => saga.State).HasConversion<string>().HasMaxLength(64);
        builder.Ignore(saga => saga.DomainEvents);
    }
}
