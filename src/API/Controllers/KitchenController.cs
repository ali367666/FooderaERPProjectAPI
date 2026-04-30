using Application.Kitchen.Commands.MarkReady;
using Application.Kitchen.Commands.StartPreparation;
using Application.Kitchen.Queries.GetKitchenLines;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.Constants;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KitchenController : ControllerBase
{
    private readonly IMediator _mediator;

    public KitchenController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [Authorize(Policy = AppPermissions.KitchenView)]
    [HttpGet("{restaurantId:int}")]
    public async Task<IActionResult> GetKitchenOrders(int restaurantId)
    {
        var result = await _mediator.Send(new GetKitchenLinesQuery(restaurantId));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.KitchenStartPreparing)]
    [HttpPut("start/{orderLineId:int}")]
    public async Task<IActionResult> StartPreparation(int orderLineId)
    {
        await _mediator.Send(new StartKitchenOrderLineCommand(orderLineId));
        return NoContent();
    }

    [Authorize(Policy = AppPermissions.KitchenMarkReady)]
    [HttpPut("ready/{orderLineId:int}")]
    public async Task<IActionResult> MarkReady(int orderLineId)
    {
        await _mediator.Send(new MarkKitchenOrderLineReadyCommand(orderLineId));
        return NoContent();
    }
}