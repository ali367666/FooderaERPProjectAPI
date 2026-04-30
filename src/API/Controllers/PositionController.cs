using Application.Common.Responce;
using Application.Position.Dtos;
using Application.Positions.Commands.Create;
using Application.Positions.Commands.Delete;
using Application.Positions.Commands.Update;
using Application.Positions.Queries.GetAll;
using Application.Positions.Queries.GetById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.Constants;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PositionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PositionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = AppPermissions.PositionCreate)]
    [HttpPost]
    public async Task<ActionResult<BaseResponse>> Create([FromBody] CreatePositionRequest request)
    {
        var result = await _mediator.Send(new CreatePositionCommand(request));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.PositionUpdate)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<BaseResponse>> Update(int id, [FromBody] UpdatePositionRequest request)
    {
        var result = await _mediator.Send(new UpdatePositionCommand(id, request));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.PositionDelete)]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<BaseResponse>> Delete(int id)
    {
        var result = await _mediator.Send(new DeletePositionCommand(id));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.PositionView)]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BaseResponse<PositionResponse>>> GetById(int id)
    {
        var result = await _mediator.Send(new GetPositionByIdQuery(id));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.PositionView)]
    [HttpGet]
    public async Task<ActionResult<BaseResponse<List<PositionResponse>>>> GetAll(
        [FromQuery] int? companyId)
    {
        var companyIdFromClaim = User.FindFirst("companyId")?.Value
            ?? User.FindFirst("CompanyId")?.Value;

        var effectiveCompanyId = companyId;
        if (effectiveCompanyId is null && int.TryParse(companyIdFromClaim, out var parsedCompanyId))
            effectiveCompanyId = parsedCompanyId;

        if (effectiveCompanyId is null || effectiveCompanyId <= 0)
            return BadRequest(BaseResponse<List<PositionResponse>>.Fail("Valid companyId is required."));

        var result = await _mediator.Send(new GetAllPositionsQuery(effectiveCompanyId.Value));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}