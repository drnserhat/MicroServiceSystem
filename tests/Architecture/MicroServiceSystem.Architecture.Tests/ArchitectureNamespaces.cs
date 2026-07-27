namespace MicroServiceSystem.Architecture.Tests;

internal static class ArchitectureNamespaces
{
    internal const string EntityFrameworkCore = "Microsoft.EntityFrameworkCore";

    internal const string MongoDriver = "MongoDB.Driver";

    internal const string Redis = "StackExchange.Redis";

    internal const string RabbitMq = "RabbitMQ.Client";

    internal const string Dapper = "Dapper";

    internal const string AspNetCore = "Microsoft.AspNetCore";

    internal const string MediatR = "MediatR";

    internal const string Npgsql = "Npgsql";

    internal static readonly string[] PersistenceAndTransportLibraries =
    [
        EntityFrameworkCore,
        MongoDriver,
        Redis,
        RabbitMq,
        Dapper,
        Npgsql
    ];
}
