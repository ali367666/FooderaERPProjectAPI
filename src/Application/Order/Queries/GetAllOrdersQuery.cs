using Application.Orders.Dtos;
using MediatR;

namespace Application.Orders.Queries.GetAll;

/// <summary>
/// Narrows the order list in the database. All members are optional; <see cref="ExcludeFinished"/> drops paid and cancelled orders.
/// <see cref="Period"/> (today / this week / this month / custom, with <see cref="From"/> and <see cref="To"/> as calendar days) keeps the
/// orders paid in that period; the days follow the company's business day (midnight when it works by the day, 06:00 in shifts).
/// A date without a period is a custom period; one date alone is that one day. The resolved UTC bounds go into
/// <see cref="PaidFromUtc"/> / <see cref="PaidToUtc"/> (end exclusive).
/// </summary>
public record OrderListFilter(
    IReadOnlyCollection<Domain.Enums.OrderStatus>? Statuses = null,
    Domain.Enums.RestaurantTableType? TableType = null,
    int? RestaurantId = null,
    bool ExcludeFinished = false,
    Domain.Enums.ReportPeriod? Period = null,
    DateTime? From = null,
    DateTime? To = null,
    DateTime? PaidFromUtc = null,
    DateTime? PaidToUtc = null);

public record GetAllOrdersQuery(OrderListFilter? Filter = null) : IRequest<List<OrderResponse>>;