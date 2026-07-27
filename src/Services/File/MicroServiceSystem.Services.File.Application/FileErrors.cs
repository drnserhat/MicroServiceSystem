using MicroServiceSystem.SharedKernel.Results;
namespace MicroServiceSystem.Services.File.Application;
public static class FileErrors
{
    public static readonly Error NotFound = Error.NotFound("file.not_found", "FileAsset was not found.");
}
