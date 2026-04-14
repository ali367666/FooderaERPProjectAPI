using Application.Kitchen.Commands.MarkReady;
using Application.Kitchen.Commands.StartPreparation;
using Application.Kitchen.Queries.GetKitchenLines;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    [Authorize(Policy = "KitchenView")]
    [HttpGet("{restaurantId}")]
    public async Task<IActionResult> GetKitchenLines(int restaurantId)
    {
        var result = await _mediator.Send(new GetKitchenLinesQuery(restaurantId));
        return Ok(result);
    }
    [Authorize(Policy = "KitchenStartPreparing")]
    [HttpPut("start/{orderLineId}")]
    public async Task<IActionResult> StartPreparation(int orderLineId)
    {
        await _mediator.Send(new StartKitchenOrderLineCommand(orderLineId));
        return NoContent();
    }
    [Authorize(Policy = "KitchenMarkReady")]
    [HttpPut("ready/{orderLineId}")]
    public async Task<IActionResult> MarkReady(int orderLineId)
    {
        await _mediator.Send(new MarkKitchenOrderLineReadyCommand(orderLineId));
        return NoContent();
    }
}