using Gateway.Api;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults(GatewayConstants.ServiceName);

builder.Services.AddHttpClient("gateway-swagger", client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

WebApplication app = builder.Build();

app.UseServiceDefaults();

app.MapGet("/", () => Results.Redirect("/swagger"))
    .AllowAnonymous()
    .ExcludeFromDescription();

app.MapGatewaySwaggerDocuments();
app.MapReverseProxy().AllowAnonymous();

await app.RunAsync();

public static class GatewayConstants
{
    public const string ServiceName = "gateway";
}

public partial class Program;
