using Application.AuditLogs.Dtos.Response;
using Application.AuditLogs.Queries.GetAll;
using Application.AuditLogs.Queries.GetById;
using Application.Common.Responce;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.Constants;
using Microsoft.Extensions.Logging;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuditLogsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuditLogsController> _logger;

    public AuditLogsController(IMediator mediator, ILogger<AuditLogsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Policy = AppPermissions.AuditLogView)]
    public async Task<ActionResult<BaseResponse<List<AuditLogResponse>>>> GetAll(
        [FromQuery] string? entityName,
        [FromQuery] string? entityId,
        [FromQuery] string? actionType,
        [FromQuery] int? userId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] string? search)
    {
        var permissionClaims = User.Claims
            .Where(c => c.Type == "Permission")
            .Select(c => c.Value)
            .ToList();
        _logger.LogInformation("AuditLogs/GetAll user permission claims: {Claims}", string.Join(", ", permissionClaims));

        var result = await _mediator.Send(
            new GetAuditLogsQuery(entityName, entityId, actionType, userId, fromUtc, toUtc, search));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [Authorize(Policy = AppPermissions.AuditLogView)]
    public async Task<ActionResult<BaseResponse<AuditLogResponse>>> GetById(long id)
    {
        var permissionClaims = User.Claims
            .Where(c => c.Type == "Permission")
            .Select(c => c.Value)
            .ToList();
        _logger.LogInformation("AuditLogs/GetById user permission claims: {Claims}", string.Join(", ", permissionClaims));

        var result = await _mediator.Send(new GetAuditLogByIdQuery(id));

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}