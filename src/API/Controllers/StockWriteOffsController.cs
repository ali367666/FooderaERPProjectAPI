using Application.Common.Responce;
using Application.StockWriteOffs;
using Application.StockWriteOffs.Commands;
using Application.StockWriteOffs.Queries;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FooderaERP.Api.Controllers;

/// <summary>Goods taken out of a warehouse as waste, spoiled or broken.</summary>
[ApiController]
[Route("api/[controller]")]
public class StockWriteOffsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StockWriteOffsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Write-offs between two days (inclusive), with what each one cost, when and by whom.</summary>
    [Authorize(Policy = AppPermissions.StockWriteOffView)]
    [HttpGet]
    public async Task<ActionResult<StockWriteOffListResponse>> GetAll(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? warehouseId)
    {
        return Ok(await _mediator.Send(new GetStockWriteOffsQuery(from, to, warehouseId)));
    }

    /// <summary>Writes goods off: the warehouse stock drops. A purchase's debt is not touched.</summary>
    [Authorize(Policy = AppPermissions.StockWriteOffCreate)]
    [HttpPost]
    public async Task<ActionResult<BaseResponse>> Create([FromBody] CreateStockWriteOffRequest request)
    {
        var result = await _mediator.Send(new CreateStockWriteOffCommand(request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
