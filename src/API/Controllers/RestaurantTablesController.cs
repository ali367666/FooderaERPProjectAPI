using Application.RestaurantTable.Dtos.Request;
using Application.RestaurantTables.Commands.Create;
using Application.RestaurantTables.Commands.Delete;
using Application.RestaurantTables.Commands.Update;
using Application.RestaurantTables.Dtos;
using Application.RestaurantTables.Queries.GetAll;
using Application.RestaurantTables.Queries.GetById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.Constants;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RestaurantTablesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RestaurantTablesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = AppPermissions.RestaurantTableCreate)]
    [HttpPost]
    public async Task<ActionResult<RestaurantTableResponse>> Create(
        [FromBody] CreateRestaurantTableCommand command,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = AppPermissions.RestaurantTableUpdate)]
    [HttpPut("{id}")]
    public async Task<ActionResult<RestaurantTableResponse>> Update(
        int id,
        [FromBody] UpdateRestaurantTableRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateRestaurantTableCommand
        {
            Id = id,
            Request = request
        };

        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = AppPermissions.RestaurantTableView)]
    [HttpGet("{id}")]
    public async Task<ActionResult<RestaurantTableResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetRestaurantTableByIdQuery { Id = id }, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = AppPermissions.RestaurantTableView)]
    [HttpGet]
    public async Task<ActionResult<List<RestaurantTableResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetAllRestaurantTablesQuery(), cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = AppPermissions.RestaurantTableDelete)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteRestaurantTableCommand { Id = id }, cancellationToken);
        return NoContent();
    }
}