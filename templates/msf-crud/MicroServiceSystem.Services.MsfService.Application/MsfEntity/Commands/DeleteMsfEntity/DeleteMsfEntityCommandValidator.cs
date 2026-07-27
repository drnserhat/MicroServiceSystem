using FluentValidation;

namespace MicroServiceSystem.Services.MsfService.Application.MsfEntity.Commands.DeleteMsfEntity;

public sealed class DeleteMsfEntityCommandValidator : AbstractValidator<DeleteMsfEntityCommand>
{
    public DeleteMsfEntityCommandValidator() => RuleFor(command => command.Id).NotEmpty();
}
