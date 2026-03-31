using Application.Common.Responce;
using Application.WarehouseStock.Commands.Create;
using Application.WarehouseStock.Commands.Delete;
using Application.WarehouseStock.Commands.Update;
using Application.WarehouseStock.Dtos.Request;
using Application.WarehouseStock.Dtos.Response;
using Application.WarehouseStock.Queries.GetById;
using Application.WarehouseStock.Queries.GetByWarehouseId;
using Application.WarehouseStock.Queries.Search;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WarehouseStockController : ControllerBase
{
    private readonly IMediator _mediator;

    public WarehouseStockController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Authorize(Policy = "WarehouseStockCreate")]
    public async Task<ActionResult<BaseResponse>> Create([FromBody] CreateWarehouseStockRequest request)
    {
        var result = await _mediator.Send(new CreateWarehouseStockCommand(request));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("warehouse/{warehouseId:int}")]
    [Authorize(Policy = "WarehouseStockView")]
    public async Task<ActionResult<BaseResponse<List<WarehouseStockResponse>>>> GetByWarehouseId(
        int warehouseId,
        [FromQuery] string? search)
    {
        var result = await _mediator.Send(new GetWarehouseStocksByWarehouseIdQuery(warehouseId, search));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "WarehouseStockView")]
    public async Task<ActionResult<BaseResponse<WarehouseStockResponse>>> GetById(int id)
    {
        var result = await _mediator.Send(new GetWarehouseStockByIdQuery(id));

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [HttpGet("search")]
    [Authorize(Policy = "WarehouseStockView")]
    public async Task<ActionResult<BaseResponse<List<WarehouseStockResponse>>>> Search(
        [FromQuery] int companyId,
        [FromQuery] string? search)
    {
        var result = await _mediator.Send(new SearchWarehouseStocksQuery(companyId, search));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPatch("{id:int}")]
    [Authorize(Policy = "WarehouseStockUpdate")]
    public async Task<ActionResult<BaseResponse>> Update(
        int id,
        [FromBody] UpdateWarehouseStockRequest request)
    {
        var result = await _mediator.Send(new UpdateWarehouseStockCommand(id, request));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "WarehouseStockDelete")]
    public async Task<ActionResult<BaseResponse>> Delete(int id)
    {
        var result = await _mediator.Send(new DeleteWarehouseStockCommand(id));

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}