using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Authorization;
using MicroServiceSystem.BuildingBlocks.MultiTenancy.Abstractions;
using MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Controllers;
using MicroServiceSystem.Services.User.Persistence;
using Npgsql;

namespace MicroServiceSystem.Services.User.Api.Controllers;

/// <summary>
/// Internal endpoint used by Identity provisioner to apply User EF migrations to a branch database.
/// Accepts host metadata + SecretRef only (no passwords on the wire).
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tenant-databases")]
[TenantIndependent]
public sealed class TenantDatabasesController(
    IConfiguration configuration,
    IServiceProvider serviceProvider) : ApiControllerBase
{
    [AuthorizeInternalService]
    [HttpPost("ensure-migrated")]
    public async Task<IActionResult> EnsureMigrated(
        [FromBody] EnsureMigratedRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Host)
            || string.IsNullOrWhiteSpace(request.DatabaseName)
            || string.IsNullOrWhiteSpace(request.Username)
            || string.IsNullOrWhiteSpace(request.SecretRef)
            || request.Port is < 1 or > 65535)
        {
            return BadRequest(new { title = "Invalid ensure-migrated payload." });
        }

        string? password = configuration[request.SecretRef];
        if (string.IsNullOrWhiteSpace(password))
        {
            return Problem(
                detail: $"SecretRef '{request.SecretRef}' is not configured.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        NpgsqlConnectionStringBuilder builder = new()
        {
            Host = request.Host.Trim(),
            Port = request.Port,
            Database = request.DatabaseName.Trim(),
            Username = request.Username.Trim(),
            Password = password
        };

        DbContextDependencies dependencies = serviceProvider.GetRequiredService<DbContextDependencies>();
        DbContextOptionsBuilder<UserDbContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql(builder.ConnectionString, npgsql =>
        {
            npgsql.MigrationsHistoryTable("__ef_migrations_history", UserDbContext.DefaultSchema);
        });

        await using UserDbContext context = new(optionsBuilder.Options, dependencies);
        await context.Database.MigrateAsync(cancellationToken);
        return Ok(new { migrated = true, database = request.DatabaseName });
    }
}

public sealed record EnsureMigratedRequest(
    string Host,
    int Port,
    string DatabaseName,
    string Username,
    string SecretRef);
