using Application.WarehouseTransfer.Commands.Delete;
using Application.WarehouseTransfer.Queries.GetAll;
using Application.WarehouseTransfer.Queries.GetById;
using Application.WarehouseTransfers.Commands.Create;
using Application.WarehouseTransfers.Dtos.Request;
using MediatR;
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

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWarehouseTransferRequest request)
    {
        var result = await _mediator.Send(new CreateWarehouseTransferCommand(request));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _mediator.Send(new GetWarehouseTransferByIdQuery(id));

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetAllWarehouseTransfersQuery());
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _mediator.Send(new DeleteWarehouseTransferCommand(id));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}