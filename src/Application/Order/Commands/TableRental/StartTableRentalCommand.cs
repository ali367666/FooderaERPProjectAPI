using Application.Orders.Dtos;
using MediatR;

namespace Application.Orders.Commands.TableRental;

public record StartTableRentalCommand(int OrderId) : IRequest<OrderResponse>;
