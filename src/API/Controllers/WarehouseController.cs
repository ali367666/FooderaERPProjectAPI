using Application.Warehouse.Commands.Create;
using Application.Warehouse.Commands.Delete;
using Application.Warehouse.Commands.Patch;
using Application.Warehouse.Commands.Update;
using Application.Warehouse.Dtos.Request;
using Application.Warehouse.Queries.GetAll;
using Application.Warehouse.Queries.GetByCompanyId;
using Application.Warehouse.Queries.GetByDriverUserId;
using Application.Warehouse.Queries.GetById;
using Application.Warehouse.Queries.GetByRestaurantId;
using Application.Warehouse.Queries.Search;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class WarehousesController : ControllerBase
{
    private readonly IMediator _mediator;

    public WarehousesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Authorize(Policy ="WarehouseCreate" )]
    public async Task<IActionResult> Create([FromBody] CreateWarehouseRequest request)
    {
        var result = await _mediator.Send(new CreateWarehouseCommand(request));

        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy ="WarehouseUpdate")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateWarehouseRequest request)
    {
        var result = await _mediator.Send(new UpdateWarehouseCommand(id, request));

        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy ="WarehouseDelete")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _mediator.Send(new DeleteWarehouseCommand(id));

        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "WarehouseView")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _mediator.Send(new GetWarehouseByIdQuery(id));

        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet]
    [Authorize(Policy = "WarehouseView")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetAllWarehousesQuery());

        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("by-company/{companyId:int}")]
    [Authorize(Policy = "WarehouseView")]
    public async Task<IActionResult> GetByCompanyId(int companyId)
    {
        var result = await _mediator.Send(new GetWarehousesByCompanyIdQuery(companyId));

        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("by-restaurant/{restaurantId:int}")]
    [Authorize(Policy = "WarehouseView")]
    public async Task<IActionResult> GetByRestaurantId(int restaurantId)
    {
        var result = await _mediator.Send(new GetWarehousesByRestaurantIdQuery(restaurantId));

        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("by-driver/{driverUserId:int}")]
    [Authorize(Policy = "WarehouseView")]
    public async Task<IActionResult> GetByDriverUserId(int driverUserId)
    {
        var result = await _mediator.Send(new GetWarehousesByDriverUserIdQuery(driverUserId));

        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("{id:int}")]
    [Authorize(Policy = "WarehouseUpdate")]
    public async Task<IActionResult> Patch(int id, [FromBody] PatchWarehouseRequest request)
    {
        var result = await _mediator.Send(new PatchWarehouseCommand(id, request));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
    [HttpGet("search")]
    [Authorize(Policy = "WarehouseView")]
    public async Task<IActionResult> Search([FromQuery] int companyId, [FromQuery] string? search)
    {
        var result = await _mediator.Send(new SearchWarehousesQuery(companyId, search));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}