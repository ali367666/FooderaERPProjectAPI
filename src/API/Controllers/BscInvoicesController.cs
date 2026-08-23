using Application.BscInvoice.Dtos;
using Application.BscInvoice.Queries.GetAll;
using Application.Common.Responce;
using Domain.Constants;
using Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FooderaERP.Api.Controllers;

[ApiController]
[Route("api/bsc-invoices")]
public class BscInvoicesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly BscInvoiceSyncService _syncService;

    public BscInvoicesController(IMediator mediator, BscInvoiceSyncService syncService)
    {
        _mediator = mediator;
        _syncService = syncService;
    }

    [Authorize(Policy = AppPermissions.BscInvoiceView)]
    [HttpGet]
    public async Task<ActionResult<BaseResponse<List<BscInvoiceMResponse>>>> GetAll(
        [FromQuery] DateTime? docDate = null)
    {
        var result = await _mediator.Send(new GetAllBscInvoicesQuery(docDate));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.BscInvoiceSync)]
    [HttpPost("sync")]
    public async Task<ActionResult> Sync([FromQuery] DateTime? date = null, CancellationToken cancellationToken = default)
    {
        var (inserted, skipped) = await _syncService.SyncAsync(date, cancellationToken);
        return Ok(new { inserted, skipped, date = (date ?? DateTime.Today.AddDays(-1)).ToString("yyyy-MM-dd") });
    }
}
