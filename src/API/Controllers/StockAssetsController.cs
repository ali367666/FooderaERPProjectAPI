using Application.Common.Responce;
using Application.StockAssets;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FooderaERP.Api.Controllers;

/// <summary>
/// Non-food goods the business keeps (plates, glasses, cutlery, detergent): the stock of each, and every increase and decrease
/// (purchase, breakage, expense, manual). Decreases are written with <c>POST /api/StockWriteOffs</c>.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StockAssetsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StockAssetsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>The company's non-food stock items with the total and per-warehouse stock and the value.</summary>
    [Authorize(Policy = AppPermissions.WarehouseStockView)]
    [HttpGet]
    public async Task<ActionResult<StockAssetListResponse>> GetAll()
        => Ok(await _mediator.Send(new GetStockAssetsQuery()));

    /// <summary>Every increase and decrease of non-food items in a period, with who did it and why.</summary>
    [Authorize(Policy = AppPermissions.WarehouseStockView)]
    [HttpGet("history")]
    public async Task<ActionResult<List<StockAssetOperationResponse>>> GetHistory(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? stockItemId)
        => Ok(await _mediator.Send(new GetStockAssetHistoryQuery(from, to, stockItemId)));

    /// <summary>Raises the stock of a non-food item by hand (a gift, a found item, a correction).</summary>
    [Authorize(Policy = AppPermissions.StockWriteOffCreate)]
    [HttpPost("increase")]
    public async Task<ActionResult<BaseResponse>> Increase([FromBody] CreateStockAssetIncreaseRequest request)
    {
        var result = await _mediator.Send(new CreateStockAssetIncreaseCommand(request));
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
