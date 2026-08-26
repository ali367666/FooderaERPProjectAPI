using Application.Shift.Commands;
using Application.Shift.Dtos;
using Application.Shift.Queries;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ShiftsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ShiftsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("open")]
    public async Task<ActionResult<ShiftResponse>> Open([FromBody] OpenShiftRequest request)
    {
        var result = await _mediator.Send(new OpenShiftCommand(request));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.PosZReport)]
    [HttpPost("{id:int}/close")]
    public async Task<ActionResult<ZReportResponse>> Close(int id, [FromBody] CloseShiftRequest request)
    {
        var result = await _mediator.Send(new CloseShiftCommand(id, request));
        return Ok(result);
    }

    [HttpGet("current")]
    public async Task<ActionResult<ShiftResponse?>> Current([FromQuery] int restaurantId)
    {
        var result = await _mediator.Send(new GetCurrentShiftQuery(restaurantId));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.PosZReport)]
    [HttpGet("{id:int}/z-report")]
    public async Task<ActionResult<ZReportResponse>> ZReport(int id)
    {
        var result = await _mediator.Send(new GetZReportQuery(id));
        return Ok(result);
    }
}
