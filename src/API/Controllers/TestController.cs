using Application.Common.Interfaces;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TestController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;

    public TestController(
        INotificationService notificationService,
        IEmailService emailService)
    {
        _notificationService = notificationService;
        _emailService = emailService;
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

    [HttpPost("send-email")]
    public async Task<IActionResult> SendEmail(CancellationToken cancellationToken)
    {
        var body = EmailTemplateBuilder.BuildBasicTemplate(
            "Test Email",
            "Bu Foodera ERP test mailidir.");

        await _emailService.SendAsync(
            "aliahmadov366@gmail.com",
            "Foodera ERP Test Mail",
            body,
            cancellationToken);

        return Ok("Mail gonderildi.");
    }
}