using FluentValidation;
using FluentValidation.Results;
using MediatR;
using MicroServiceSystem.BuildingBlocks.Application.Results;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Runs FluentValidation validators before the handler executes. Handlers therefore never validate
/// input themselves and expected validation failures are returned as a failed result, not thrown.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        IValidator<TRequest>[] applicableValidators = [.. validators];

        if (applicableValidators.Length == 0)
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        ValidationResult[] validationResults = await Task.WhenAll(
            applicableValidators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        ValidationFailure[] failures = [.. validationResults.SelectMany(result => result.Errors).Where(failure => failure is not null)];

        if (failures.Length == 0)
        {
            return await next();
        }

        Dictionary<string, string[]> groupedFailures = failures
            .GroupBy(failure => failure.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);

        Error error = FrameworkErrors.Validation(groupedFailures);

        return ResultFactory.IsResultType(typeof(TResponse))
            ? ResultFactory.CreateFailure<TResponse>(error)
            : throw new ValidationException(failures);
    }
}
