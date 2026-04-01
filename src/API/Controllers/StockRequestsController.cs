using Application.Common.Responce;
using Application.StockRequests.Commands.Create;
using Application.StockRequests.Dtos.Request;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockRequestsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StockRequestsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<BaseResponse<int>>> Create([FromBody] CreateStockRequestRequest request)
    {
        var result = await _mediator.Send(new CreateStockRequestCommand(request));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}