using Application.Common.Responce;
using Application.WarehouseStock.Dtos.Response;
using Application.WarehouseStock.Queries.SearchBalances;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.Constants;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WarehouseStockBalanceController : ControllerBase
{
    private readonly IMediator _mediator;

    public WarehouseStockBalanceController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("search")]
    [Authorize(Policy = AppPermissions.WarehouseStockView)]
    public async Task<ActionResult<BaseResponse<List<WarehouseStockBalanceResponse>>>> Search(
        [FromQuery] int companyId,
        [FromQuery] int? warehouseId,
        [FromQuery] int? stockItemId,
        [FromQuery] string? search)
    {
        var result = await _mediator.Send(new SearchWarehouseStockBalancesQuery(
            companyId,
            warehouseId,
            stockItemId,
            search));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
