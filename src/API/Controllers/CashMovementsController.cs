using Application.CashMovement.Commands;
using Application.CashMovement.Dtos;
using Application.CashMovement.Queries;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CashMovementsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CashMovementsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = AppPermissions.CashRegisterView)]
    [HttpGet]
    public async Task<ActionResult<List<CashMovementResponse>>> GetAll(
        [FromQuery] int restaurantId, [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var result = await _mediator.Send(new GetAllCashMovementsQuery(restaurantId, from, to));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.CashRegisterManage)]
    [HttpPost]
    public async Task<ActionResult<CashMovementResponse>> Create([FromBody] CreateCashMovementRequest request)
    {
        var result = await _mediator.Send(new CreateCashMovementCommand(request));
        return Ok(result);
    }
}
