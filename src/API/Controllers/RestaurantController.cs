using Application.Restaurant.Commands.Create;
using Application.Restaurant.Dtos.Request;
using Application.Restaurant.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class RestaurantController : ControllerBase
{
    private readonly IMediator _mediator;

    public RestaurantController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = "RestaurantCreate")]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateRestaurantRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateRestaurantCommand(request);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = "RestaurantView")]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var query = new GetRestaurantByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [Authorize(Policy = "RestaurantView")]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var query = new GetAllRestaurantsQuery();
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = "RestaurantView")]
    [HttpGet("company/{companyId:int}")]
    public async Task<IActionResult> GetByCompanyId(
        [FromRoute] int companyId,
        CancellationToken cancellationToken)
    {
        var query = new GetRestaurantsByCompanyIdQuery(companyId);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [Authorize(Policy = "RestaurantUpdate")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] UpdateRestaurantRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateRestaurantCommand(id, request);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = "RestaurantDelete")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteRestaurantCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}