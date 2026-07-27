using FluentValidation;
using MicroServiceSystem.Services.MsfService.Domain.Aggregates;

namespace MicroServiceSystem.Services.MsfService.Application.MsfEntity.Commands.UpdateMsfEntity;

public sealed class UpdateMsfEntityCommandValidator : AbstractValidator<UpdateMsfEntityCommand>
{
    public UpdateMsfEntityCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(MsfEntityConstraints.NameMaxLength);

        RuleFor(command => command.Description)
            .MaximumLength(MsfEntityConstraints.DescriptionMaxLength);
    }
}
