using Application.SaleReturn;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SaleReturnsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SaleReturnsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Looks up a paid sale by the barcode printed on its receipt.</summary>
    [Authorize(Policy = AppPermissions.PosReturnSale)]
    [HttpGet("lookup")]
    public async Task<ActionResult<ReturnableOrderResponse>> Lookup([FromQuery] string code)
    {
        var result = await _mediator.Send(new FindReturnableOrderQuery(code));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.PosReturnSale)]
    [HttpPost]
    public async Task<ActionResult<SaleReturnResponse>> Create([FromBody] CreateSaleReturnRequest request)
    {
        var result = await _mediator.Send(new CreateSaleReturnCommand(request));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.PosReturnSale)]
    [HttpGet]
    public async Task<ActionResult<List<SaleReturnResponse>>> GetAll(
        [FromQuery] int restaurantId, [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var result = await _mediator.Send(new GetSaleReturnsQuery(restaurantId, from, to));
        return Ok(result);
    }
}
