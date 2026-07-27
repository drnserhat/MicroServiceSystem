using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MicroServiceSystem.BuildingBlocks.Authorization;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Controllers;
using MicroServiceSystem.Services.File.Application;
using MicroServiceSystem.SharedKernel.Constants;

namespace MicroServiceSystem.Services.File.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/files")]
public sealed class FilesController(ISender sender) : ApiControllerBase
{
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [HasPermission(FrameworkPermissions.FileAssetsUpload)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] string container,
        [FromForm] Guid tenantId,
        CancellationToken cancellationToken)
    {
        await using Stream stream = file.OpenReadStream();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);

        return ToActionResult(
            await sender.Send(
                new UploadFileCommand(file.FileName, file.ContentType, memory.ToArray(), container, tenantId),
                cancellationToken));
    }
}
