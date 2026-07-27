using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MicroServiceSystem.BuildingBlocks.Authorization;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Controllers;
using MicroServiceSystem.Services.File.Application;
using MicroServiceSystem.SharedKernel.Constants;
using MicroServiceSystem.SharedKernel.Results;

namespace MicroServiceSystem.Services.File.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/files")]
public sealed class FilesController(ISender sender) : ApiControllerBase
{
    private const long MaxUploadBytes = 10 * 1024 * 1024;

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [HasPermission(FrameworkPermissions.FileAssetsUpload)]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] string container,
        CancellationToken cancellationToken)
    {
        if (file.Length > MaxUploadBytes)
        {
            return ToActionResult(
                Result.Failure<FileAssetResponse>(
                    FrameworkErrors.Validation(
                        new Dictionary<string, string[]>
                        {
                            [nameof(file)] = [$"The upload exceeds the {MaxUploadBytes} byte limit."]
                        })));
        }

        await using Stream stream = file.OpenReadStream();
        using var memory = new MemoryStream(capacity: (int)file.Length);
        await stream.CopyToAsync(memory, cancellationToken);

        return ToActionResult(
            await sender.Send(
                new UploadFileCommand(file.FileName, file.ContentType, memory.ToArray(), container),
                cancellationToken));
    }
}
