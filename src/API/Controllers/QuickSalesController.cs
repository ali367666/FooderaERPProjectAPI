using Application.QuickSales.Commands.Create;
using Application.QuickSales.Dtos;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FooderaERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/quick-sales")]
public class QuickSalesController : ControllerBase
{
    private readonly IMediator _mediator;

    public QuickSalesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = AppPermissions.OrdersCreate)]
    [Authorize(Policy = AppPermissions.OrdersPay)]
    [HttpPost]
    public async Task<ActionResult<QuickSaleResponse>> Create([FromBody] QuickSaleRequest request)
    {
        var result = await _mediator.Send(new QuickSaleCommand(request));
        return Ok(result);
    }
}
