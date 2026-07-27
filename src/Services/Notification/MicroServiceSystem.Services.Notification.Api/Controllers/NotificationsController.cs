using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MicroServiceSystem.BuildingBlocks.Authorization;
using MicroServiceSystem.BuildingBlocks.ServiceDefaults.Controllers;
using MicroServiceSystem.Services.Notification.Application;
using MicroServiceSystem.SharedKernel.Constants;

namespace MicroServiceSystem.Services.Notification.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications")]
public sealed class NotificationsController(ISender sender) : ApiControllerBase
{
    [HttpPost]
    [HasPermission(FrameworkPermissions.NotificationMessagesCreate)]
    public async Task<IActionResult> Create(CreateNotificationCommand command, CancellationToken cancellationToken) =>
        ToActionResult(await sender.Send(command, cancellationToken));
}
