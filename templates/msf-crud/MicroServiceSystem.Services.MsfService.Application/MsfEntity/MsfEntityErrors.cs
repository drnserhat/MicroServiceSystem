using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.MsfService.Application.MsfEntity;

public static class MsfEntityErrors
{
    public static Error NotFound(Guid id) => FrameworkErrors.NotFound(nameof(MsfEntity), id);

    public static Error NameAlreadyExists(string name) =>
        Error.Conflict("msfentity.name_already_exists", $"An entry named '{name}' already exists.");
}
