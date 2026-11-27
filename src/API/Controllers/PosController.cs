using Application.Pos.Dtos;
using Application.Pos.Queries.Search;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PosController : ControllerBase
{
    private readonly IMediator _mediator;

    public PosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// The tables-screen search box: every typed word may appear anywhere in a table's name, section
    /// or note, or in an open order's number, receipt, waiter, customer, note or delivery details.
    /// Answers with the matching tables and the matching open orders; a blank <c>q</c> returns nothing.
    /// </summary>
    [Authorize(Policy = AppPermissions.OrdersView)]
    [HttpGet("search")]
    public async Task<ActionResult<PosSearchResponse>> Search(
        [FromQuery] string? q,
        [FromQuery] int? restaurantId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SearchPosQuery { Term = q, RestaurantId = restaurantId },
            cancellationToken);

        return Ok(result);
    }
}
