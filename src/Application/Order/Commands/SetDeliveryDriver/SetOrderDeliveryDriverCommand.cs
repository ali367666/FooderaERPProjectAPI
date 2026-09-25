using Application.Orders.Dtos;
using MediatR;

namespace Application.Orders.Commands.SetDeliveryDriver;

/// <summary>Assigns (or clears, when null) the courier employee for a delivery order.</summary>
public record SetOrderDeliveryDriverCommand(int OrderId, int? DriverEmployeeId) : IRequest<OrderResponse>;
