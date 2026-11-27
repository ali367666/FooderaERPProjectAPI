using Application.Orders.Dtos;
using MediatR;

namespace Application.Orders.Commands.SetBanquet;

/// <summary>
/// Turns the order into a banquet order (or back). Only possible while the order has no products,
/// because the products on it are priced for one mode or the other.
/// </summary>
public record SetOrderBanquetCommand(int OrderId, bool Banquet) : IRequest<OrderResponse>;
