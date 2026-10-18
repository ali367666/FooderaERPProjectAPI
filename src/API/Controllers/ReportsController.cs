using Application.Reports.Dtos;
using Application.Reports.Queries;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }


    [Authorize(Policy = AppPermissions.ReportsView)]
    [HttpGet("business")]
    public async Task<ActionResult<BusinessReportResponse>> GetBusinessReport(
        [FromQuery] BusinessReportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetBusinessReportQuery(request), cancellationToken);
        return Ok(result);
    }
}
