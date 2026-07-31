using Gateway.Api;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults(GatewayConstants.ServiceName);

builder.Services.AddHttpClient("gateway-swagger", client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddHttpClient("gateway-health", client =>
{
    client.Timeout = TimeSpan.FromSeconds(3);
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

WebApplication app = builder.Build();

app.UseServiceDefaults();

app.MapGet("/", () => Results.Redirect("/swagger"))
    .AllowAnonymous()
    .ExcludeFromDescription();

app.MapGatewaySwaggerDocuments();

app.MapControllers();

// Fallback policy requires JWT. Anonymous public routes are allowlisted via
// ReverseProxy:Routes:*:AuthorizationPolicy = "Anonymous" (login, refresh only).
app.MapReverseProxy();

await app.RunAsync();

public static class GatewayConstants
{
    public const string ServiceName = "gateway";
}

public partial class Program;
