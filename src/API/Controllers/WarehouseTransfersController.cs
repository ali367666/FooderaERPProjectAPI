using Application.Common.Responce;
using Application.WarehouseTransfer.Commands.Approve;
using Application.WarehouseTransfer.Commands.Cancel;
using Application.WarehouseTransfer.Commands.Delete;
using Application.WarehouseTransfer.Commands.Dispatch;
using Application.WarehouseTransfer.Commands.Receive;
using Application.WarehouseTransfer.Commands.Reject;
using Application.WarehouseTransfer.Commands.Submit;
using Application.WarehouseTransfer.Commands.Update;
using Application.WarehouseTransfer.Queries.GetAll;
using Application.WarehouseTransfer.Queries.GetById;
using Application.WarehouseTransfers.Commands.Create;
using Application.WarehouseTransfers.Dtos.Request;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FooderaERP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WarehouseTransfersController : ControllerBase
{
    private readonly IMediator _mediator;

    public WarehouseTransfersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = "WarehouseTransferCreate")]
    [HttpPost]
    public async Task<ActionResult<BaseResponse<int>>> Create([FromBody] CreateWarehouseTransferRequest request)
    {
        var result = await _mediator.Send(new CreateWarehouseTransferCommand(request));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = "WarehouseTransferView")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BaseResponse>> GetById(int id)
    {
        var result = await _mediator.Send(new GetWarehouseTransferByIdQuery(id));

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [Authorize(Policy = "WarehouseTransferView")]
    [HttpGet]
    public async Task<ActionResult<BaseResponse>> GetAll()
    {
        var result = await _mediator.Send(new GetAllWarehouseTransfersQuery());

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = "WarehouseTransferUpdate")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<BaseResponse>> Update(
        int id,
        [FromBody] UpdateWarehouseTransferRequest request)
    {
        var result = await _mediator.Send(new UpdateWarehouseTransferCommand(id, request));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = "WarehouseTransferDelete")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<BaseResponse>> Delete(int id)
    {
        var result = await _mediator.Send(new DeleteWarehouseTransferCommand(id));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = "WarehouseTransferSubmit")]
    [HttpPost("{id:int}/submit")]
    public async Task<ActionResult<BaseResponse>> Submit(int id)
    {
        var result = await _mediator.Send(new SubmitWarehouseTransferCommand(id));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = "WarehouseTransferApprove")]
    [HttpPost("{id:int}/approve")]
    public async Task<ActionResult<BaseResponse>> Approve(int id)
    {
        var result = await _mediator.Send(new ApproveWarehouseTransferCommand(id));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = "WarehouseTransferDispatch")]
    [HttpPost("{id:int}/dispatch")]
    public async Task<ActionResult<BaseResponse>> Dispatch(int id)
    {
        var result = await _mediator.Send(new DispatchWarehouseTransferCommand(id));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = "WarehouseTransferReceive")]
    [HttpPost("{id:int}/receive")]
    public async Task<ActionResult<BaseResponse>> Receive(int id)
    {
        var result = await _mediator.Send(new ReceiveWarehouseTransferCommand(id));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = "WarehouseTransferCancel")]
    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<BaseResponse>> Cancel(int id)
    {
        var result = await _mediator.Send(new CancelWarehouseTransferCommand(id));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = "WarehouseTransferReject")]
    [HttpPost("{id:int}/reject")]
    public async Task<ActionResult<BaseResponse>> Reject(int id)
    {
        var result = await _mediator.Send(new RejectWarehouseTransferCommand(id));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("{id:int}/approve-from-mail")]
    public async Task<IActionResult> ApproveFromMail(int id)
    {
        var result = await _mediator.Send(new ApproveWarehouseTransferCommand(id));

        if (!result.Success)
            return BadRequest(result.Message);

        return Content("Warehouse transfer approved successfully.");
    }

    [AllowAnonymous]
    [HttpGet("{id:int}/reject-from-mail")]
    public async Task<IActionResult> RejectFromMail(int id)
    {
        var result = await _mediator.Send(new RejectWarehouseTransferCommand(id));

        if (!result.Success)
            return BadRequest(result.Message);

        return Content("Warehouse transfer rejected successfully.");
    }
}