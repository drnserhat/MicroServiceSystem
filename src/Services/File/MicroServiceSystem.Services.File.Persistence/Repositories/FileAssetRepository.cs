using Microsoft.EntityFrameworkCore;
using MicroServiceSystem.BuildingBlocks.Persistence.Repositories;
using MicroServiceSystem.Services.File.Application.Abstractions;
using MicroServiceSystem.Services.File.Domain.Aggregates;
namespace MicroServiceSystem.Services.File.Persistence.Repositories;
public sealed class FileAssetRepository(FileDbContext context) : EfRepository<FileAsset, Guid>(context), IFileAssetRepository
{
    public async Task<IReadOnlyList<FileAsset>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking().ToListAsync(cancellationToken);
}
