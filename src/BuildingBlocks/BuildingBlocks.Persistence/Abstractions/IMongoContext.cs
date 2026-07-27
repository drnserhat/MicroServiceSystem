using MongoDB.Driver;

namespace MicroServiceSystem.BuildingBlocks.Persistence.Abstractions;

public interface IMongoContext
{
    IMongoDatabase Database { get; }

    IMongoCollection<TDocument> Collection<TDocument>(string name);
}
