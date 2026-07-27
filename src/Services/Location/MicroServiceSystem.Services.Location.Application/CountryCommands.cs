using FluentValidation;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.Services.Location.Application.Abstractions;
using MicroServiceSystem.Services.Location.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Pagination;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.Location.Application;

public sealed record CountryResponse(Guid Id, string Code, string Name, uint Version);

public sealed record GetCountryByCodeQuery(string Code) : IQuery<CountryResponse>;

public sealed class GetCountryByCodeQueryValidator : AbstractValidator<GetCountryByCodeQuery>
{
    public GetCountryByCodeQueryValidator() => RuleFor(query => query.Code).Length(2, 3);
}

public sealed class GetCountryByCodeQueryHandler(ICountryRepository countries)
    : IQueryHandler<GetCountryByCodeQuery, CountryResponse>
{
    public async Task<Result<CountryResponse>> Handle(
        GetCountryByCodeQuery query,
        CancellationToken cancellationToken)
    {
        Country? country = await countries.FindByCodeAsync(query.Code, cancellationToken);
        return country is null ? LocationErrors.NotFound : ToResponse(country);
    }

    private CountryResponse ToResponse(Country country) =>
        new(country.Id, country.Code, country.Name, countries.GetConcurrencyVersion(country));
}

public sealed record ListCountriesQuery(PaginationRequest Pagination) : IQuery<PagedResult<CountryResponse>>;

public sealed class ListCountriesQueryValidator : AbstractValidator<ListCountriesQuery>
{
    public ListCountriesQueryValidator()
    {
        RuleFor(query => query.Pagination.PageNumber).GreaterThanOrEqualTo(PaginationDefaults.FirstPageNumber);
        RuleFor(query => query.Pagination.PageSize).InclusiveBetween(1, PaginationDefaults.MaxPageSize);
    }
}

public sealed class ListCountriesQueryHandler(ICountryRepository countries)
    : IQueryHandler<ListCountriesQuery, PagedResult<CountryResponse>>
{
    public async Task<Result<PagedResult<CountryResponse>>> Handle(
        ListCountriesQuery query,
        CancellationToken cancellationToken)
    {
        PagedResult<Country> page = await countries.PagedListAsync(query.Pagination, cancellationToken);
        return page.Project(country => new CountryResponse(
            country.Id,
            country.Code,
            country.Name,
            countries.GetConcurrencyVersion(country)));
    }
}

// Tenant comes from the caller's token, not the request body.
public sealed record CreateCountryCommand(string Code, string Name) : ICommand<CountryResponse>;

public sealed class CreateCountryCommandValidator : AbstractValidator<CreateCountryCommand>
{
    public CreateCountryCommandValidator()
    {
        RuleFor(command => command.Code).Length(2, 3);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(128);
    }
}

public sealed class CreateCountryCommandHandler(
    ICountryRepository countries,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateCountryCommand, CountryResponse>
{
    public async Task<Result<CountryResponse>> Handle(
        CreateCountryCommand command,
        CancellationToken cancellationToken)
    {
        if (await countries.FindByCodeAsync(command.Code, cancellationToken) is not null)
        {
            return LocationErrors.CodeAlreadyExists;
        }

        Country country = Country.Create(command.Code, command.Name);
        await countries.AddAsync(country, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CountryResponse(
            country.Id,
            country.Code,
            country.Name,
            countries.GetConcurrencyVersion(country));
    }
}

public sealed record UpdateCountryCommand(string Code, string Name, uint ExpectedVersion)
    : ICommand<CountryResponse>;

public sealed class UpdateCountryCommandValidator : AbstractValidator<UpdateCountryCommand>
{
    public UpdateCountryCommandValidator()
    {
        RuleFor(command => command.Code).Length(2, 3);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(128);
    }
}

public sealed class UpdateCountryCommandHandler(
    ICountryRepository countries,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateCountryCommand, CountryResponse>
{
    public async Task<Result<CountryResponse>> Handle(
        UpdateCountryCommand command,
        CancellationToken cancellationToken)
    {
        Country? country = await countries.FindByCodeAsync(command.Code, cancellationToken);

        if (country is null)
        {
            return LocationErrors.NotFound;
        }

        countries.SetExpectedConcurrencyVersion(country, command.ExpectedVersion);
        country.Rename(command.Name);
        countries.Update(country);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CountryResponse(
            country.Id,
            country.Code,
            country.Name,
            countries.GetConcurrencyVersion(country));
    }
}

public sealed record DeleteCountryCommand(string Code, uint ExpectedVersion) : ICommand;

public sealed class DeleteCountryCommandValidator : AbstractValidator<DeleteCountryCommand>
{
    public DeleteCountryCommandValidator() => RuleFor(command => command.Code).Length(2, 3);
}

public sealed class DeleteCountryCommandHandler(
    ICountryRepository countries,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteCountryCommand>
{
    public async Task<Result> Handle(DeleteCountryCommand command, CancellationToken cancellationToken)
    {
        Country? country = await countries.FindByCodeAsync(command.Code, cancellationToken);

        if (country is null)
        {
            return Result.Failure(LocationErrors.NotFound);
        }

        countries.SetExpectedConcurrencyVersion(country, command.ExpectedVersion);
        countries.Remove(country);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
