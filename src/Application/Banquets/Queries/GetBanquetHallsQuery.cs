using Application.Banquets.Dtos;
using MediatR;

namespace Application.Banquets.Queries;

/// <summary>
/// The banquet halls of a branch, each marked free or busy as a whole. Without a date and time busy means
/// busy right now; with them it means already booked for that time (<c>DurationMinutes</c> defaults to 240).
/// </summary>
public record GetBanquetHallsQuery(
    int RestaurantId,
    DateTime? Date,
    string? Time,
    int? DurationMinutes) : IRequest<List<BanquetHallResponse>>;
