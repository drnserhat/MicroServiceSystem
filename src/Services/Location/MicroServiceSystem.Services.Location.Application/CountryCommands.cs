using FluentValidation;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.Services.Location.Application.Abstractions;
using MicroServiceSystem.Services.Location.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Results;
namespace MicroServiceSystem.Services.Location.Application;
public sealed record CreateCountryCommand(string Code,string Name,Guid TenantId):ICommand<CountryResponse>;
public sealed class CreateCountryCommandValidator:AbstractValidator<CreateCountryCommand>{public CreateCountryCommandValidator(){RuleFor(x=>x.Code).Length(2,3);RuleFor(x=>x.Name).NotEmpty();RuleFor(x=>x.TenantId).NotEmpty();}}
public sealed record CountryResponse(Guid Id,string Code,string Name);
public sealed class CreateCountryCommandHandler(ICountryRepository countries,ICurrentTenant tenant):ICommandHandler<CreateCountryCommand,CountryResponse>{public async Task<Result<CountryResponse>> Handle(CreateCountryCommand c,CancellationToken ct){using IDisposable scope=tenant.Change(c.TenantId);var country=Country.Create(c.Code,c.Name);country.TenantId=c.TenantId;await countries.AddAsync(country,ct);return new CountryResponse(country.Id,country.Code,country.Name);}}
public sealed record ListCountriesQuery:IQuery<IReadOnlyList<CountryResponse>>;
public sealed class ListCountriesQueryHandler(ICountryRepository countries):IQueryHandler<ListCountriesQuery,IReadOnlyList<CountryResponse>>{public async Task<Result<IReadOnlyList<CountryResponse>>> Handle(ListCountriesQuery q,CancellationToken ct){var items=await countries.ListAllAsync(ct);return items.Select(x=>new CountryResponse(x.Id,x.Code,x.Name)).ToList();}}
