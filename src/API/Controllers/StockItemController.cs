using Application.StockItem.Commands.Create;
using Application.StockItem.Commands.Delete;
using Application.StockItem.Commands.Patch;
using Application.StockItem.Commands.Update;
using Application.StockItem.Dtos.Request;
using Application.StockItem.Queries;
using Application.StockItem.Queries.GetByCompanyId;
using Application.StockItem.Queries.GetById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class StockItemController : ControllerBase
{
    private readonly IMediator _mediator;

    public StockItemController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Authorize(Policy ="StockItemCreate")]
    public async Task<IActionResult> Create([FromBody] StockItemRequest request)
    {
        var result = await _mediator.Send(new CreateStockItemCommand(request));
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "StockItemUpdate")]
    public async Task<IActionResult> Update(int id, [FromBody] StockItemRequest request)
    {
        var result = await _mediator.Send(new UpdateStockItemCommand(id, request));
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "StockItemDelete")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _mediator.Send(new DeleteStockItemCommand(id));
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "StockItemView")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _mediator.Send(new GetStockItemByIdQuery(id));
        return Ok(result);
    }

    [HttpGet("company/{companyId}")]
    [Authorize(Policy = "StockItemView")]
    public async Task<IActionResult> GetByCompanyId(int companyId)
    {
        var result = await _mediator.Send(new GetStockItemsByCompanyIdQuery(companyId));
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Policy = "StockItemView")]
    public async Task<IActionResult> GetAll([FromQuery] GetAllStockItemsRequest request)
    {
        var result = await _mediator.Send(new GetAllStockItemsQuery(request));
        return Ok(result);
    }
    [HttpPatch("{id}")]
    [Authorize(Policy = "StockItemUpdate")]
    public async Task<IActionResult> Patch(int id, [FromBody] PatchStockItemRequest request)
    {
        var result = await _mediator.Send(new PatchStockItemCommand(id, request));
        return Ok(result);
    }
}