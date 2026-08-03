using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.BuildingBlocks.Localization.Abstractions;

/// <summary>
/// Maps stable <see cref="Error.Code"/> values to culture-specific descriptions.
/// </summary>
public interface IErrorLocalizer
{
    Error Localize(Error error);
}
