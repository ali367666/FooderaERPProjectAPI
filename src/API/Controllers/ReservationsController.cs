using Application.Reservations.Commands.ChangeStatus;
using Application.Reservations.Commands.Create;
using Application.Reservations.Commands.Delete;
using Application.Reservations.Commands.Extend;
using Application.Reservations.Commands.Move;
using Application.Reservations.Commands.Update;
using Application.Reservations.Dtos;
using Application.Reservations.Queries;
using Application.RestaurantTables.Dtos;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ReservationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Policy = AppPermissions.ReservationView)]
    public async Task<ActionResult<List<ReservationResponse>>> GetAll(
        [FromQuery] DateTime? date, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAllReservationsQuery { Date = date }, ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = AppPermissions.ReservationCreate)]
    public async Task<ActionResult<ReservationResponse>> Create(
        [FromBody] CreateReservationRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateReservationCommand { Request = request }, ct);
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AppPermissions.ReservationUpdate)]
    public async Task<ActionResult<ReservationResponse>> Update(
        int id, [FromBody] UpdateReservationRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateReservationCommand { Id = id, Request = request }, ct);
        return Ok(result);
    }

    [HttpPost("{id}/status")]
    public async Task<ActionResult<ReservationResponse>> ChangeStatus(
        int id, [FromQuery] string action, CancellationToken ct)
    {
        var result = await _mediator.Send(new ChangeReservationStatusCommand { Id = id, Action = action }, ct);
        return Ok(result);
    }

    /// <summary>Holds the table longer: adds <paramref name="minutes"/> to the reservation's duration.</summary>
    [HttpPost("{id}/extend")]
    public async Task<ActionResult<ReservationResponse>> Extend(
        int id, [FromQuery] int minutes, CancellationToken ct)
    {
        var result = await _mediator.Send(new ExtendReservationCommand { Id = id, Minutes = minutes }, ct);
        return Ok(result);
    }

    /// <summary>Tables of the reservation's branch that are free for its date, time and duration.</summary>
    [HttpGet("{id}/available-tables")]
    [Authorize(Policy = AppPermissions.ReservationView)]
    public async Task<ActionResult<List<RestaurantTableResponse>>> GetAvailableTables(
        int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAvailableTablesForReservationQuery { ReservationId = id }, ct);
        return Ok(result);
    }

    /// <summary>Moves the reservation to another table of the same branch.</summary>
    [HttpPost("{id}/move")]
    public async Task<ActionResult<ReservationResponse>> Move(
        int id, [FromQuery] int tableId, CancellationToken ct)
    {
        var result = await _mediator.Send(new MoveReservationCommand { Id = id, TableId = tableId }, ct);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AppPermissions.ReservationDelete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteReservationCommand { Id = id }, ct);
        return NoContent();
    }
}
