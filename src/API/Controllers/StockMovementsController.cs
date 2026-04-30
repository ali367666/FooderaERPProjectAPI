using Application.Common.Responce;
using Application.StockMovements.Dtos.Response;
using Application.StockMovements.Queries.SearchStockMovements;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.Constants;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StockMovementsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StockMovementsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("search")]
    [Authorize(Policy = AppPermissions.WarehouseStockView)]
    public async Task<ActionResult<BaseResponse<List<StockMovementListItemResponse>>>> Search(
        [FromQuery] int companyId,
        [FromQuery] string? search)
    {
        var result = await _mediator.Send(new SearchStockMovementsQuery(companyId, search));
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }
}
