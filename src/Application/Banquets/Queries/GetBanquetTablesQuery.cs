using Application.Banquets.Dtos;
using MediatR;

namespace Application.Banquets.Queries;

/// <summary>
/// The tables of a hall for the banquet screen, each marked free or busy. Without a date and time
/// "busy" means busy right now (open order, reservation inside the block window); with them it means
/// already booked for that time.
/// </summary>
public record GetBanquetTablesQuery(
    int RestaurantId,
    int? SectionId,
    DateTime? Date,
    string? Time,
    int? DurationMinutes) : IRequest<List<BanquetTableResponse>>;
