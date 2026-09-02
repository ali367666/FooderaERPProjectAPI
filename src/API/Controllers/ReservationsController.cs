using Application.Reservations.Commands.ChangeStatus;
using Application.Reservations.Commands.Create;
using Application.Reservations.Commands.Delete;
using Application.Reservations.Commands.Update;
using Application.Reservations.Dtos;
using Application.Reservations.Queries;
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

    [HttpDelete("{id}")]
    [Authorize(Policy = AppPermissions.ReservationDelete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteReservationCommand { Id = id }, ct);
        return NoContent();
    }
}
