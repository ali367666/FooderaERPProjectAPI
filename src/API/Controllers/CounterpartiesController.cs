using Application.Counterparty.Commands;
using Application.Counterparty.Dtos;
using Application.Counterparty.Queries;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CounterpartiesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CounterpartiesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = AppPermissions.CounterpartyView)]
    [HttpGet]
    public async Task<ActionResult<List<CounterpartyResponse>>> GetAll()
    {
        var result = await _mediator.Send(new GetAllCounterpartiesQuery());
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.CounterpartyCreate)]
    [HttpPost]
    public async Task<ActionResult<CounterpartyResponse>> Create([FromBody] CreateCounterpartyRequest request)
    {
        var result = await _mediator.Send(new CreateCounterpartyCommand(request));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.CounterpartyUpdate)]
    [HttpPut]
    public async Task<ActionResult<CounterpartyResponse>> Update([FromBody] UpdateCounterpartyRequest request)
    {
        var result = await _mediator.Send(new UpdateCounterpartyCommand(request));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.CounterpartyDelete)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _mediator.Send(new DeleteCounterpartyCommand(id));
        return Ok();
    }

    [Authorize(Policy = AppPermissions.CounterpartyUpdate)]
    [HttpPost("{id:int}/adjust-debt")]
    public async Task<ActionResult<CounterpartyResponse>> AdjustDebt(int id, [FromBody] AdjustCounterpartyDebtRequest request)
    {
        var result = await _mediator.Send(new AdjustCounterpartyDebtCommand(id, request));
        return Ok(result);
    }
}
