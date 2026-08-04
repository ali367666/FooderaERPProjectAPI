using Application.Common.Responce;
using Application.StockPurchase.Commands.Approve;
using Application.StockPurchase.Commands.Create;
using Application.StockPurchase.Commands.Delete;
using Application.StockPurchase.Commands.Reject;
using Application.StockPurchase.Commands.Submit;
using Application.StockPurchase.Commands.Update;
using Application.StockPurchase.Dtos.Request;
using Application.StockPurchase.Dtos.Response;
using Application.StockPurchase.Queries.GetAll;
using Application.StockPurchase.Queries.GetById;
using Application.StockPurchase.Queries.GetExchangeRate;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FooderaERP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockPurchasesController : ControllerBase
{
    private readonly IMediator _mediator;

    public StockPurchasesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = AppPermissions.StockPurchaseView)]
    [HttpGet]
    public async Task<ActionResult<BaseResponse<List<StockPurchaseResponse>>>> GetAll()
    {
        var result = await _mediator.Send(new GetAllStockPurchasesQuery());
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.StockPurchaseView)]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BaseResponse<StockPurchaseResponse>>> GetById(int id)
    {
        var result = await _mediator.Send(new GetStockPurchaseByIdQuery(id));
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.StockPurchaseCreate)]
    [HttpPost]
    public async Task<ActionResult<BaseResponse<int>>> Create([FromBody] CreateStockPurchaseRequest request)
    {
        var result = await _mediator.Send(new CreateStockPurchaseCommand(request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.StockPurchaseUpdate)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<BaseResponse>> Update(int id, [FromBody] UpdateStockPurchaseRequest request)
    {
        var result = await _mediator.Send(new UpdateStockPurchaseCommand(id, request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.StockPurchaseDelete)]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<BaseResponse>> Delete(int id)
    {
        var result = await _mediator.Send(new DeleteStockPurchaseCommand(id));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.StockPurchaseSubmit)]
    [HttpPost("{id:int}/submit")]
    public async Task<ActionResult<BaseResponse>> Submit(int id)
    {
        var result = await _mediator.Send(new SubmitStockPurchaseCommand(id));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.StockPurchaseApprove)]
    [HttpPost("{id:int}/approve")]
    public async Task<ActionResult<BaseResponse>> Approve(int id)
    {
        var result = await _mediator.Send(new ApproveStockPurchaseCommand(id));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.StockPurchaseReject)]
    [HttpPost("{id:int}/reject")]
    public async Task<ActionResult<BaseResponse>> Reject(int id)
    {
        var result = await _mediator.Send(new RejectStockPurchaseCommand(id));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>CBAR məzənnəsi — valyuta kodu və tarix göndər, AZN məzənnəsi gəlir.</summary>
    [Authorize(Policy = AppPermissions.StockPurchaseCreate)]
    [HttpGet("exchange-rate")]
    public async Task<ActionResult<BaseResponse<decimal>>> GetExchangeRate(
        [FromQuery] string currency,
        [FromQuery] DateTime? date)
    {
        var result = await _mediator.Send(
            new GetCbarExchangeRateQuery(currency, date ?? DateTime.UtcNow));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
