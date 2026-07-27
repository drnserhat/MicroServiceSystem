using MicroServiceSystem.Services.File.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
namespace MicroServiceSystem.Services.File.Application.Abstractions;
public interface IFileAssetRepository : IRepository<FileAsset, Guid>
{
    Task<IReadOnlyList<FileAsset>> ListAllAsync(CancellationToken cancellationToken = default);
}
