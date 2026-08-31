using Application.CounterpartyCategory.Commands;
using Application.CounterpartyCategory.Dtos;
using Application.CounterpartyCategory.Queries;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CounterpartyCategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CounterpartyCategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = AppPermissions.CounterpartyView)]
    [HttpGet]
    public async Task<ActionResult<List<CounterpartyCategoryResponse>>> GetAll()
    {
        var result = await _mediator.Send(new GetAllCounterpartyCategoriesQuery());
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.CounterpartyCreate)]
    [HttpPost]
    public async Task<ActionResult<CounterpartyCategoryResponse>> Create([FromBody] CreateCounterpartyCategoryRequest request)
    {
        var result = await _mediator.Send(new CreateCounterpartyCategoryCommand(request));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.CounterpartyUpdate)]
    [HttpPut]
    public async Task<ActionResult<CounterpartyCategoryResponse>> Update([FromBody] UpdateCounterpartyCategoryRequest request)
    {
        var result = await _mediator.Send(new UpdateCounterpartyCategoryCommand(request));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.CounterpartyDelete)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _mediator.Send(new DeleteCounterpartyCategoryCommand(id));
        return Ok();
    }
}
