using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Extensions;
using MicroServiceSystem.Services.Audit.Application;
using MicroServiceSystem.Services.Audit.Infrastructure;
using MicroServiceSystem.Services.Audit.Persistence;
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults(AuditConstants.ServiceName);
builder.Services.AddAuditApplication(builder.Configuration);
builder.Services.AddAuditPersistence(builder.Configuration);
builder.Services.AddAuditInfrastructure(builder.Configuration);
WebApplication app = builder.Build();
app.UseServiceDefaults();
app.MapControllers();
await app.RunAsync();
public static class AuditConstants { public const string ServiceName = "audit"; }
public partial class Program;
