using Application.QrMenu.Commands;
using Application.QrMenu.Dtos;
using Application.QrMenu.Queries;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Managing the QR menu: each table has its own QR link (<c>/menu/{restaurantId}?t={qrToken}</c>) and the
/// menu behind it only changes when someone presses "update QR".
/// </summary>
[Authorize]
[ApiController]
[Route("api/qr-menu")]
public class QrMenuController : ControllerBase
{
    private readonly IMediator _mediator;

    public QrMenuController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Last publish time, whether the menu has changed since, and every table's QR token.</summary>
    [Authorize(Policy = AppPermissions.MenuItemView)]
    [HttpGet("restaurants/{restaurantId:int}")]
    public async Task<ActionResult<QrMenuStatusResponse>> GetStatus(int restaurantId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetQrMenuStatusQuery(restaurantId), cancellationToken);
        return Ok(result);
    }

    /// <summary>"Update QR": the QR codes start showing the current products and prices.</summary>
    [Authorize(Policy = AppPermissions.MenuItemUpdate)]
    [HttpPost("restaurants/{restaurantId:int}/publish")]
    public async Task<ActionResult<QrMenuStatusResponse>> Publish(int restaurantId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new PublishQrMenuCommand(restaurantId), cancellationToken);
        return Ok(result);
    }
}
