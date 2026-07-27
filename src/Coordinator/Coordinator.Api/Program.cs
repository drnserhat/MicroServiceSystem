using Coordinator.Application;
using Coordinator.Infrastructure;
using Coordinator.Persistence;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults(CoordinatorConstants.ServiceName);

builder.Services.AddCoordinatorApplication(builder.Configuration);
builder.Services.AddCoordinatorPersistence(builder.Configuration);
builder.Services.AddCoordinatorInfrastructure(builder.Configuration);

WebApplication app = builder.Build();

app.UseServiceDefaults();
app.MapControllers();

await app.RunAsync();

public static class CoordinatorConstants
{
    public const string ServiceName = "coordinator";
}

public partial class Program;
