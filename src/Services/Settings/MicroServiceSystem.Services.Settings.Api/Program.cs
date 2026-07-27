using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Extensions;
using MicroServiceSystem.Services.Settings.Application;
using MicroServiceSystem.Services.Settings.Infrastructure;
using MicroServiceSystem.Services.Settings.Persistence;
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults(SettingsConstants.ServiceName);
builder.Services.AddSettingsApplication(builder.Configuration);
builder.Services.AddSettingsPersistence(builder.Configuration);
builder.Services.AddSettingsInfrastructure(builder.Configuration);
WebApplication app = builder.Build();
app.UseServiceDefaults();
app.MapControllers();
await app.RunAsync();
public static class SettingsConstants { public const string ServiceName = "settings"; }
public partial class Program;
