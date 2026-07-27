using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Extensions;
using MicroServiceSystem.Services.Identity.Application;
using MicroServiceSystem.Services.Identity.Infrastructure;
using MicroServiceSystem.Services.Identity.Persistence;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults(IdentityConstants.ServiceName);

builder.Services.AddIdentityApplication(builder.Configuration);
builder.Services.AddIdentityPersistence(builder.Configuration);
builder.Services.AddIdentityInfrastructure(builder.Configuration);

WebApplication app = builder.Build();

app.UseServiceDefaults();
app.MapControllers();

await app.RunAsync();

public static class IdentityConstants
{
    public const string ServiceName = "identity";
}

public partial class Program;
