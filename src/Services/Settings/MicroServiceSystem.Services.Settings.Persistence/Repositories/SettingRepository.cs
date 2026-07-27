using Microsoft.EntityFrameworkCore;
using MicroServiceSystem.BuildingBlocks.Persistence.Repositories;
using MicroServiceSystem.Services.Settings.Application.Abstractions;
using MicroServiceSystem.Services.Settings.Domain.Aggregates;
namespace MicroServiceSystem.Services.Settings.Persistence.Repositories;
public sealed class SettingRepository(SettingsDbContext context):EfRepository<Setting,Guid>(context),ISettingRepository{public Task<Setting?> FindByKeyAsync(string key,CancellationToken ct=default)=>Set.FirstOrDefaultAsync(x=>x.Key==key.Trim(),ct);public async Task<IReadOnlyList<Setting>> ListAllAsync(CancellationToken ct=default)=>await Set.AsNoTracking().ToListAsync(ct);}
