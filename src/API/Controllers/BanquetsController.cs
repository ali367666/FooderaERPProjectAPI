using Application.Banquets.Commands;
using Application.Banquets.Dtos;
using Application.Banquets.Queries;
using Application.Orders.Dtos;
using Application.Reservations.Dtos;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Banquets: pick a hall, pick its tables, then either open one bill on them right away or book them
/// for a date and open the banquet that day.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BanquetsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BanquetsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// The tables of a hall (<c>sectionId</c>) marked free or busy. Without <c>date</c>/<c>time</c> busy
    /// means busy now; with them it means already booked then (<c>durationMinutes</c> defaults to 240).
    /// </summary>
    [Authorize(Policy = AppPermissions.RestaurantTableView)]
    [HttpGet("tables")]
    public async Task<ActionResult<List<BanquetTableResponse>>> GetTables(
        [FromQuery] int restaurantId,
        [FromQuery] int? sectionId,
        [FromQuery] DateTime? date,
        [FromQuery] string? time,
        [FromQuery] int? durationMinutes,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetBanquetTablesQuery(restaurantId, sectionId, date, time, durationMinutes), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// The banquet halls of a branch, free or busy as a whole. Without <c>date</c>/<c>time</c> busy means busy
    /// now; with them it means already booked then (<c>durationMinutes</c> defaults to 240).
    /// </summary>
    [Authorize(Policy = AppPermissions.RestaurantTableView)]
    [HttpGet("halls")]
    public async Task<ActionResult<List<BanquetHallResponse>>> GetHalls(
        [FromQuery] int restaurantId,
        [FromQuery] DateTime? date,
        [FromQuery] string? time,
        [FromQuery] int? durationMinutes,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetBanquetHallsQuery(restaurantId, date, time, durationMinutes), cancellationToken);
        return Ok(result);
    }

    /// <summary>Opens a banquet order now on the chosen tables: one bill, banquet menu and prices.</summary>
    [Authorize(Policy = AppPermissions.OrdersCreate)]
    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Open(
        [FromBody] OpenBanquetRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new OpenBanquetCommand(request), cancellationToken);
        return Ok(result);
    }

    /// <summary>Books a banquet hall (all its tables) for a date and time; send <c>sectionId</c>, no table choice is needed.</summary>
    [Authorize(Policy = AppPermissions.ReservationCreate)]
    [HttpPost("reservations")]
    public async Task<ActionResult<ReservationResponse>> CreateReservation(
        [FromBody] CreateBanquetReservationRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateBanquetReservationCommand(request), cancellationToken);
        return Ok(result);
    }

    /// <summary>The banquet day: opens the booked banquet as an order on its tables and seats the booking.</summary>
    [Authorize(Policy = AppPermissions.OrdersCreate)]
    [HttpPost("reservations/{id:int}/open")]
    public async Task<ActionResult<OrderResponse>> OpenReservation(
        int id, [FromBody] OpenBanquetReservationRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new OpenBanquetReservationCommand(id, request.WaiterId), cancellationToken);
        return Ok(result);
    }
}
