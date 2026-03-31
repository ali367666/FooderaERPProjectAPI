using Application.AuditLogs.Dtos.Response;
using Application.AuditLogs.Queries.GetAll;
using Application.Common.Responce;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuditLogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuditLogsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(Policy = "AuditLogView")]
    public async Task<ActionResult<BaseResponse<List<AuditLogResponse>>>> GetAll(
        [FromQuery] string? entityName,
        [FromQuery] string? entityId,
        [FromQuery] string? actionType)
    {
        var result = await _mediator.Send(
            new GetAuditLogsQuery(entityName, entityId, actionType));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}