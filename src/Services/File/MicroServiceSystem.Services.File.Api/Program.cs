using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Extensions;
using MicroServiceSystem.Services.File.Application;
using MicroServiceSystem.Services.File.Infrastructure;
using MicroServiceSystem.Services.File.Persistence;
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults(FileConstants.ServiceName);
builder.Services.AddFileApplication(builder.Configuration);
builder.Services.AddFilePersistence(builder.Configuration);
builder.Services.AddFileInfrastructure(builder.Configuration);
WebApplication app = builder.Build();
app.UseServiceDefaults();
app.MapControllers();
await app.RunAsync();
public static class FileConstants { public const string ServiceName = "file"; }
public partial class Program;
