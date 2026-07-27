using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Extensions;
using MicroServiceSystem.Services.Notification.Application;
using MicroServiceSystem.Services.Notification.Infrastructure;
using MicroServiceSystem.Services.Notification.Persistence;
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults(NotificationConstants.ServiceName);
builder.Services.AddNotificationApplication(builder.Configuration);
builder.Services.AddNotificationPersistence(builder.Configuration);
builder.Services.AddNotificationInfrastructure(builder.Configuration);
WebApplication app = builder.Build();
app.UseServiceDefaults();
app.MapControllers();
await app.RunAsync();
public static class NotificationConstants { public const string ServiceName = "notification"; }
public partial class Program;
