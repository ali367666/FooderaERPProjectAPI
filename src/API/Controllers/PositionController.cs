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

    [Authorize(Policy = "PositionCreate")]
    [HttpPost]
    public async Task<ActionResult<BaseResponse>> Create([FromBody] CreatePositionRequest request)
    {
        var result = await _mediator.Send(new CreatePositionCommand(request));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = "PositionUpdate")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<BaseResponse>> Update(int id, [FromBody] UpdatePositionRequest request)
    {
        var result = await _mediator.Send(new UpdatePositionCommand(id, request));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = "PositionDelete")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<BaseResponse>> Delete(int id)
    {
        var result = await _mediator.Send(new DeletePositionCommand(id));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = "PositionView")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BaseResponse<PositionResponse>>> GetById(int id)
    {
        var result = await _mediator.Send(new GetPositionByIdQuery(id));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = "PositionView")]

    [HttpGet]
    public async Task<ActionResult<BaseResponse<List<PositionResponse>>>> GetAll()
    {
        var result = await _mediator.Send(new GetAllPositionsQuery());

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}