using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Extensions;
using MicroServiceSystem.Services.MsfService.Application;
using MicroServiceSystem.Services.MsfService.Infrastructure;
using MicroServiceSystem.Services.MsfService.Persistence;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults(MsfServiceConstants.ServiceName);

builder.Services.AddMsfServiceApplication(builder.Configuration);
builder.Services.AddMsfServicePersistence(builder.Configuration);
builder.Services.AddMsfServiceInfrastructure(builder.Configuration);

WebApplication app = builder.Build();

app.UseServiceDefaults();
app.MapControllers();

await app.RunAsync();

public static class MsfServiceConstants
{
    public const string ServiceName = "msfservice";
}
