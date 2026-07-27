using FluentValidation;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.Services.Settings.Application.Abstractions;
using MicroServiceSystem.Services.Settings.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Pagination;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.Settings.Application;

public sealed record SettingResponse(Guid Id, string Key, string Value, uint Version);

public sealed record GetSettingByKeyQuery(string Key) : IQuery<SettingResponse>;

public sealed class GetSettingByKeyQueryValidator : AbstractValidator<GetSettingByKeyQuery>
{
    public GetSettingByKeyQueryValidator() => RuleFor(query => query.Key).NotEmpty().MaximumLength(128);
}

public sealed class GetSettingByKeyQueryHandler(ISettingRepository settings)
    : IQueryHandler<GetSettingByKeyQuery, SettingResponse>
{
    public async Task<Result<SettingResponse>> Handle(GetSettingByKeyQuery query, CancellationToken cancellationToken)
    {
        Setting? setting = await settings.FindByKeyAsync(query.Key, cancellationToken);
        return setting is null ? SettingsErrors.NotFound : ToResponse(setting);
    }

    private SettingResponse ToResponse(Setting setting) =>
        new(setting.Id, setting.Key, setting.Value, settings.GetConcurrencyVersion(setting));
}

public sealed record ListSettingsQuery(PaginationRequest Pagination) : IQuery<PagedResult<SettingResponse>>;

public sealed class ListSettingsQueryValidator : AbstractValidator<ListSettingsQuery>
{
    public ListSettingsQueryValidator()
    {
        RuleFor(query => query.Pagination.PageNumber).GreaterThanOrEqualTo(PaginationDefaults.FirstPageNumber);
        RuleFor(query => query.Pagination.PageSize).InclusiveBetween(1, PaginationDefaults.MaxPageSize);
    }
}

public sealed class ListSettingsQueryHandler(ISettingRepository settings)
    : IQueryHandler<ListSettingsQuery, PagedResult<SettingResponse>>
{
    public async Task<Result<PagedResult<SettingResponse>>> Handle(
        ListSettingsQuery query,
        CancellationToken cancellationToken)
    {
        PagedResult<Setting> page = await settings.PagedListAsync(query.Pagination, cancellationToken);
        return page.Project(setting => new SettingResponse(
            setting.Id,
            setting.Key,
            setting.Value,
            settings.GetConcurrencyVersion(setting)));
    }
}

// Tenant comes from the caller's token, not the request body.
public sealed record UpsertSettingCommand(string Key, string Value, uint? ExpectedVersion)
    : ICommand<SettingResponse>;

public sealed class UpsertSettingCommandValidator : AbstractValidator<UpsertSettingCommand>
{
    public UpsertSettingCommandValidator()
    {
        RuleFor(command => command.Key).NotEmpty().MaximumLength(128);
        RuleFor(command => command.Value).NotEmpty();
    }
}

public sealed class UpsertSettingCommandHandler(
    ISettingRepository settings,
    IUnitOfWork unitOfWork) : ICommandHandler<UpsertSettingCommand, SettingResponse>
{
    public async Task<Result<SettingResponse>> Handle(
        UpsertSettingCommand command,
        CancellationToken cancellationToken)
    {
        Setting? setting = await settings.FindByKeyAsync(command.Key, cancellationToken);

        if (setting is null)
        {
            setting = Setting.Create(command.Key, command.Value);
            await settings.AddAsync(setting, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ToResponse(setting);
        }

        if (command.ExpectedVersion is not uint expectedVersion)
        {
            return SettingsErrors.ConcurrencyTokenRequired;
        }

        settings.SetExpectedConcurrencyVersion(setting, expectedVersion);
        setting.SetValue(command.Value);
        settings.Update(setting);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(setting);
    }

    private SettingResponse ToResponse(Setting setting) =>
        new(setting.Id, setting.Key, setting.Value, settings.GetConcurrencyVersion(setting));
}

public sealed record DeleteSettingCommand(string Key, uint ExpectedVersion) : ICommand;

public sealed class DeleteSettingCommandValidator : AbstractValidator<DeleteSettingCommand>
{
    public DeleteSettingCommandValidator() => RuleFor(command => command.Key).NotEmpty().MaximumLength(128);
}

public sealed class DeleteSettingCommandHandler(
    ISettingRepository settings,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteSettingCommand>
{
    public async Task<Result> Handle(DeleteSettingCommand command, CancellationToken cancellationToken)
    {
        Setting? setting = await settings.FindByKeyAsync(command.Key, cancellationToken);

        if (setting is null)
        {
            return Result.Failure(SettingsErrors.NotFound);
        }

        settings.SetExpectedConcurrencyVersion(setting, command.ExpectedVersion);
        settings.Remove(setting);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
