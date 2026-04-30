using Application.Common.Responce;
using Application.StockRequests.Commands.Approve;
using Application.StockRequests.Commands.Create;
using Application.StockRequests.Commands.Delete;
using Application.StockRequests.Commands.Recall;
using Application.StockRequests.Commands.Reject;
using Application.StockRequests.Commands.Submit;
using Application.StockRequests.Commands.Update;
using Application.StockRequests.Dtos.Request;
using Application.StockRequests.Queries.GetAll;
using Application.StockRequests.Queries.GetById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.Constants;

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

    [Authorize(Policy = AppPermissions.StockRequestCreate)]
    [HttpPost]
    public async Task<ActionResult<BaseResponse<int>>> Create([FromBody] CreateStockRequestRequest request)
    {
        var result = await _mediator.Send(new CreateStockRequestCommand(request));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.StockRequestUpdate)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<BaseResponse>> Update(int id, [FromBody] UpdateStockRequestRequest request)
    {
        var result = await _mediator.Send(new UpdateStockRequestCommand(id, request));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.StockRequestSubmit)]
    [HttpPost("{id:int}/submit")]
    public async Task<ActionResult<BaseResponse>> Submit(int id)
    {
        var result = await _mediator.Send(new SubmitStockRequestCommand(id));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.StockRequestRecall)]
    [HttpPost("{id:int}/recall")]
    public async Task<ActionResult<BaseResponse>> Recall(int id)
    {
        var result = await _mediator.Send(new RecallStockRequestCommand(id));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.StockRequestApprove)]
    [HttpPost("{id:int}/approve")]
    public async Task<ActionResult<BaseResponse>> Approve(int id)
    {
        var result = await _mediator.Send(new ApproveStockRequestCommand(id));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.StockRequestReject)]
    [HttpPost("{id:int}/reject")]
    public async Task<ActionResult<BaseResponse>> Reject(int id)
    {
        var result = await _mediator.Send(new RejectStockRequestCommand(id));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.StockRequestView)]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _mediator.Send(new GetStockRequestByIdQuery(id));

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    //[Authorize(Policy = AppPermissions.StockRequestView)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetAllStockRequestsQuery());
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.StockRequestDelete)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _mediator.Send(new DeleteStockRequestCommand(id));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("{id:int}/approve-from-mail")]
    public async Task<IActionResult> ApproveFromMail(int id)
    {
        var result = await _mediator.Send(new ApproveStockRequestCommand(id));

        if (!result.Success)
        {
            return Content($@"
<html>
    <body style='font-family:Arial,sans-serif;padding:30px;'>
        <h2>Approve failed</h2>
        <p>{result.Message}</p>
    </body>
</html>", "text/html");
        }

        return Content($@"
<html>
    <body style='font-family:Arial,sans-serif;padding:30px;'>
        <h2>Stock request approved successfully</h2>
        <p>Request Id: {id}</p>
    </body>
</html>", "text/html");
    }

    [AllowAnonymous]
    [HttpGet("{id:int}/reject-from-mail")]
    public async Task<IActionResult> RejectFromMail(int id)
    {
        var result = await _mediator.Send(new RejectStockRequestCommand(id));

        if (!result.Success)
        {
            return Content($@"
<html>
    <body style='font-family:Arial,sans-serif;padding:30px;'>
        <h2>Reject failed</h2>
        <p>{result.Message}</p>
    </body>
</html>", "text/html");
        }

        return Content($@"
<html>
    <body style='font-family:Arial,sans-serif;padding:30px;'>
        <h2>Stock request rejected successfully</h2>
        <p>Request Id: {id}</p>
    </body>
</html>", "text/html");
    }
}