using Application.Finance.Commands;
using Application.Finance.Dtos;
using Application.Finance.Queries;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FinanceController : ControllerBase
{
    private readonly IMediator _mediator;

    public FinanceController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Income, expenses and profit for a week, month or year, day by day.</summary>
    [Authorize(Policy = AppPermissions.FinanceView)]
    [HttpGet("report")]
    public async Task<ActionResult<FinanceReportResponse>> GetReport(
        [FromQuery] FinanceReportRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetFinanceReportQuery(request), cancellationToken));
    }

    [Authorize(Policy = AppPermissions.FinanceView)]
    [HttpGet("entries")]
    public async Task<ActionResult<List<FinanceEntryResponse>>> GetEntries(
        [FromQuery] GetFinanceEntriesRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetFinanceEntriesQuery(request), cancellationToken));
    }

    [Authorize(Policy = AppPermissions.FinanceManage)]
    [HttpPost("entries")]
    public async Task<ActionResult<FinanceEntryResponse>> Create(
        [FromBody] SaveFinanceEntryRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new CreateFinanceEntryCommand(request), cancellationToken));
    }

    [Authorize(Policy = AppPermissions.FinanceManage)]
    [HttpPut("entries/{id:int}")]
    public async Task<ActionResult<FinanceEntryResponse>> Update(
        int id, [FromBody] SaveFinanceEntryRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new UpdateFinanceEntryCommand(id, request), cancellationToken));
    }

    [Authorize(Policy = AppPermissions.FinanceManage)]
    [HttpDelete("entries/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteFinanceEntryCommand(id), cancellationToken);
        return NoContent();
    }
}
