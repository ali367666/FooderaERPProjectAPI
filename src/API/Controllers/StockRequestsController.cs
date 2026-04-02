using Application.Common.Responce;
using Application.StockRequests.Commands.Approve;
using Application.StockRequests.Commands.Create;
using Application.StockRequests.Commands.Recall;
using Application.StockRequests.Commands.Reject;
using Application.StockRequests.Commands.Submit;
using Application.StockRequests.Commands.Update;
using Application.StockRequests.Dtos.Request;
using MediatR;
using Microsoft.AspNetCore.Mvc;


namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockRequestsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StockRequestsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<BaseResponse<int>>> Create([FromBody] CreateStockRequestRequest request)
    {
        var result = await _mediator.Send(new CreateStockRequestCommand(request));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
    [HttpPut("{id:int}")]
    public async Task<ActionResult<BaseResponse>> Update(int id, [FromBody] UpdateStockRequestRequest request)
    {
        var result = await _mediator.Send(new UpdateStockRequestCommand(id, request));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("{id:int}/submit")]
    public async Task<ActionResult<BaseResponse>> Submit(int id)
    {
        var result = await _mediator.Send(new SubmitStockRequestCommand(id));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("{id:int}/recall")]
    public async Task<ActionResult<BaseResponse>> Recall(int id)
    {
        var result = await _mediator.Send(new RecallStockRequestCommand(id));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("{id:int}/approve")]
    public async Task<ActionResult<BaseResponse>> Approve(int id)
    {
        var result = await _mediator.Send(new ApproveStockRequestCommand(id));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("{id:int}/reject")]
    public async Task<ActionResult<BaseResponse>> Reject(int id)
    {
        var result = await _mediator.Send(new RejectStockRequestCommand(id));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}