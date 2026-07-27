using FluentValidation;
using MicroServiceSystem.Services.MsfService.Domain.Aggregates;

namespace MicroServiceSystem.Services.MsfService.Application.MsfEntity.Commands.CreateMsfEntity;

public sealed class CreateMsfEntityCommandValidator : AbstractValidator<CreateMsfEntityCommand>
{
    public CreateMsfEntityCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(MsfEntityConstraints.NameMaxLength);

        RuleFor(command => command.Description)
            .MaximumLength(MsfEntityConstraints.DescriptionMaxLength);
    }
}
