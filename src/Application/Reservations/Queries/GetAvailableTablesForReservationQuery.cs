using Application.RestaurantTables.Dtos;
using MediatR;

namespace Application.Reservations.Queries;

/// <summary>Tables a reservation can be moved to: free for the reservation's own date, time and duration.</summary>
public class GetAvailableTablesForReservationQuery : IRequest<List<RestaurantTableResponse>>
{
    public int ReservationId { get; set; }
}
