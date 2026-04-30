using Application.Common.Responce;
using Application.WarehouseStock.Commands.ApproveDocument;
using Application.WarehouseStock.Commands.CreateDocument;
using Application.WarehouseStock.Commands.DeleteDocument;
using Application.WarehouseStock.Commands.UpdateDocument;
using Application.WarehouseStock.Dtos.Request;
using Application.WarehouseStock.Dtos.Response;
using Application.WarehouseStock.Queries.GetDocumentById;
using Application.WarehouseStock.Queries.GetDocumentsByWarehouseId;
using Application.WarehouseStock.Queries.SearchDocuments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.Constants;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WarehouseStockController : ControllerBase
{
    private readonly IMediator _mediator;

    public WarehouseStockController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Authorize(Policy = AppPermissions.WarehouseStockCreate)]
    public async Task<ActionResult<BaseResponse<int>>> Create([FromBody] CreateWarehouseStockDocumentRequest request)
    {
        var result = await _mediator.Send(new CreateWarehouseStockDocumentCommand(request));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("warehouse/{warehouseId:int}")]
    [Authorize(Policy = AppPermissions.WarehouseStockView)]
    public async Task<ActionResult<BaseResponse<List<WarehouseStockDocumentSummaryResponse>>>> GetByWarehouseId(
        int warehouseId,
        [FromQuery] string? search)
    {
        var result = await _mediator.Send(new GetWarehouseStockDocumentsByWarehouseIdQuery(warehouseId, search));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = AppPermissions.WarehouseStockView)]
    public async Task<ActionResult<BaseResponse<WarehouseStockDocumentDetailResponse>>> GetById(int id)
    {
        var result = await _mediator.Send(new GetWarehouseStockDocumentByIdQuery(id));

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [HttpGet("search")]
    [Authorize(Policy = AppPermissions.WarehouseStockView)]
    public async Task<ActionResult<BaseResponse<List<WarehouseStockDocumentSummaryResponse>>>> Search(
        [FromQuery] int companyId,
        [FromQuery] string? search)
    {
        var result = await _mediator.Send(new SearchWarehouseStockDocumentsQuery(companyId, search));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPatch("{id:int}")]
    [Authorize(Policy = AppPermissions.WarehouseStockUpdate)]
    public async Task<ActionResult<BaseResponse>> Update(
        int id,
        [FromBody] UpdateWarehouseStockDocumentRequest request)
    {
        var result = await _mediator.Send(new UpdateWarehouseStockDocumentCommand(id, request));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("{id:int}/approve")]
    [Authorize(Policy = AppPermissions.WarehouseStockUpdate)]
    public async Task<ActionResult<BaseResponse>> Approve(int id)
    {
        var result = await _mediator.Send(new ApproveWarehouseStockDocumentCommand(id));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = AppPermissions.WarehouseStockDelete)]
    public async Task<ActionResult<BaseResponse>> Delete(int id)
    {
        var result = await _mediator.Send(new DeleteWarehouseStockDocumentCommand(id));

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}
