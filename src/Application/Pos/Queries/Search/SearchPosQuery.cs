using Application.Pos.Dtos;
using MediatR;

namespace Application.Pos.Queries.Search;

/// <summary>
/// The POS tables-screen search box. Every typed word must appear somewhere in a table's name,
/// section or note, or in an open order's number, receipt number, waiter, customer, note, table or
/// delivery details. A blank <see cref="Term"/> returns nothing.
/// </summary>
public class SearchPosQuery : IRequest<PosSearchResponse>
{
    public string? Term { get; set; }
    public int? RestaurantId { get; set; }
}
