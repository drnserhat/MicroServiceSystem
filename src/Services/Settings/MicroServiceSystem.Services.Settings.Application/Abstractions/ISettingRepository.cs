using MicroServiceSystem.Services.Settings.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
namespace MicroServiceSystem.Services.Settings.Application.Abstractions;
public interface ISettingRepository:IRepository<Setting,Guid>{Task<Setting?> FindByKeyAsync(string key,CancellationToken cancellationToken=default);Task<IReadOnlyList<Setting>> ListAllAsync(CancellationToken cancellationToken=default);}
