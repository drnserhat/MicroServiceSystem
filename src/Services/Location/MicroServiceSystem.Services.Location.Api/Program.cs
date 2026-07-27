using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Extensions;
using MicroServiceSystem.Services.Location.Application;
using MicroServiceSystem.Services.Location.Infrastructure;
using MicroServiceSystem.Services.Location.Persistence;
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults(LocationConstants.ServiceName);
builder.Services.AddLocationApplication(builder.Configuration);
builder.Services.AddLocationPersistence(builder.Configuration);
builder.Services.AddLocationInfrastructure(builder.Configuration);
WebApplication app = builder.Build();
app.UseServiceDefaults();
app.MapControllers();
await app.RunAsync();
public static class LocationConstants { public const string ServiceName = "location"; }
public partial class Program;
