using Application.Orders.Dtos;
using Application.Orders.Dtos.Request;
using MediatR;

namespace Application.Orders.Commands.PayPart;

public record PayOrderPartCommand(int OrderId, PayOrderPartRequest Request) : IRequest<PartPaymentResponse>;
