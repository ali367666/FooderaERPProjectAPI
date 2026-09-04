using Application.Orders.Dtos;
using MediatR;

namespace Application.Orders.Commands.TableRental;

public record StopTableRentalCommand(int OrderId) : IRequest<OrderResponse>;
