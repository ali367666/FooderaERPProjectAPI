using Application.Orders.Dtos;
using MediatR;

namespace Application.Orders.Queries.GetAll;

/// <summary>Narrows the order list in the database. All members are optional; <see cref="ExcludeFinished"/> drops paid and cancelled orders.</summary>
public record OrderListFilter(
    IReadOnlyCollection<Domain.Enums.OrderStatus>? Statuses = null,
    Domain.Enums.RestaurantTableType? TableType = null,
    int? RestaurantId = null,
    bool ExcludeFinished = false);

public record GetAllOrdersQuery(OrderListFilter? Filter = null) : IRequest<List<OrderResponse>>;