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

    /// <summary>
    /// Stock items with the total and per-warehouse stock and the value: the non-food ones, or every item with <c>allTypes=true</c> (the stock page).
    /// </summary>
    [Authorize(Policy = AppPermissions.WarehouseStockView)]
    [HttpGet]
    public async Task<ActionResult<StockAssetListResponse>> GetAll([FromQuery] bool allTypes = false)
        => Ok(await _mediator.Send(new GetStockAssetsQuery(allTypes)));

    /// <summary>Every increase and decrease in a period, with who did it and why: of one item (any kind), else of the non-food items.</summary>
    [Authorize(Policy = AppPermissions.WarehouseStockView)]
    [HttpGet("history")]
    public async Task<ActionResult<List<StockAssetOperationResponse>>> GetHistory(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? stockItemId)
        => Ok(await _mediator.Send(new GetStockAssetHistoryQuery(from, to, stockItemId)));

    /// <summary>Raises the stock of an item by hand (a gift, a found item, a correction).</summary>
    [Authorize(Policy = AppPermissions.StockWriteOffCreate)]
    [HttpPost("increase")]
    public async Task<ActionResult<BaseResponse>> Increase([FromBody] CreateStockAssetIncreaseRequest request)
    {
        var result = await _mediator.Send(new CreateStockAssetIncreaseCommand(request));
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
