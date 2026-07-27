using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Extensions;
using MicroServiceSystem.Services.User.Application;
using MicroServiceSystem.Services.User.Infrastructure;
using MicroServiceSystem.Services.User.Persistence;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults(UserConstants.ServiceName);

builder.Services.AddUserApplication(builder.Configuration);
builder.Services.AddUserPersistence(builder.Configuration);
builder.Services.AddUserInfrastructure(builder.Configuration);

WebApplication app = builder.Build();

app.UseServiceDefaults();
app.MapControllers();

await app.RunAsync();

public static class UserConstants
{
    public const string ServiceName = "user";
}

public partial class Program;
