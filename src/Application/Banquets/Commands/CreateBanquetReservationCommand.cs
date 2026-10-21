using Application.Banquets.Dtos;
using Application.Reservations.Dtos;
using MediatR;

namespace Application.Banquets.Commands;

/// <summary>Books several tables of a hall for a banquet; opened later with <see cref="OpenBanquetReservationCommand"/>.</summary>
public record CreateBanquetReservationCommand(CreateBanquetReservationRequest Request) : IRequest<ReservationResponse>;
