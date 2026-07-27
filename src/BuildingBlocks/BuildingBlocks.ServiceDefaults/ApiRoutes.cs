namespace MicroServiceSystem.BuildingBlocks.ServiceDefaults;

public static class ApiRoutes
{
    public const string VersionedRoutePrefix = "api/v{version:apiVersion}";

    public const string SwaggerRoutePrefix = "swagger";

    public const string OpenApiDocumentRoute = "/openapi/{documentName}.json";
}
