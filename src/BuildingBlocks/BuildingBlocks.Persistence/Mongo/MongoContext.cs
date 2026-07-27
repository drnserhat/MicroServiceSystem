using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Persistence.Abstractions;
using MicroServiceSystem.BuildingBlocks.Persistence.Configuration;
using MongoDB.Driver;

namespace MicroServiceSystem.BuildingBlocks.Persistence.Mongo;

public sealed class MongoContext : IMongoContext
{
    public MongoContext(IMongoClient client, IOptions<MongoOptions> options)
    {
        ArgumentNullException.ThrowIfNull(client);

        Database = client.GetDatabase(options.Value.DatabaseName);
    }

    public IMongoDatabase Database { get; }

    public IMongoCollection<TDocument> Collection<TDocument>(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return Database.GetCollection<TDocument>(name);
    }
}
