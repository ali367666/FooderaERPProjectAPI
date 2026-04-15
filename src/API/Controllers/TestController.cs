using Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TestController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public TestController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send(CancellationToken cancellationToken)
    {
        await _notificationService.CreateAsync(
            userId: 1,
            companyId: 5,
            title: "Test notification",
            message: "Bu test notification-dur.",
            type: "Info",
            cancellationToken: cancellationToken);

        return Ok("Notification yaradıldı");
    }
}