using Application.Common.Responce;
using Application.Notifications.Commands.Delete;
using Application.Notifications.Commands.MarkRead;
using Application.Notifications.Commands.MarkUnread;
using Application.Notifications.Dtos.Response;
using Application.Notifications.Queries.GetNotificationById;
using Application.Notifications.Queries.GetNotifications;
using Application.Notifications.Queries.GetUnreadCount;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<BaseResponse<List<NotificationResponse>>>> GetAll(
        [FromQuery] int? companyId)
    {
        var result = await _mediator.Send(new GetNotificationsQuery(companyId));
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<BaseResponse<int>>> GetUnreadCount([FromQuery] int? companyId)
    {
        var result = await _mediator.Send(new GetUnreadNotificationCountQuery(companyId));
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BaseResponse<NotificationResponse>>> GetById(
        int id,
        [FromQuery] int? companyId)
    {
        var result = await _mediator.Send(new GetNotificationByIdQuery(id, companyId));
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    [HttpPost("{id:int}/read")]
    public async Task<ActionResult<BaseResponse>> MarkRead(int id, [FromQuery] int? companyId)
    {
        var result = await _mediator.Send(new MarkNotificationReadCommand(id, companyId));
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("{id:int}/unread")]
    public async Task<ActionResult<BaseResponse>> MarkUnread(int id, [FromQuery] int? companyId)
    {
        var result = await _mediator.Send(new MarkNotificationUnreadCommand(id, companyId));
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<BaseResponse>> Delete(int id, [FromQuery] int? companyId)
    {
        var result = await _mediator.Send(new DeleteNotificationCommand(id, companyId));
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }
}
