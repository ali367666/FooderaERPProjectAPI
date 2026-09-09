using Application.CashMovement.Dtos;
using MediatR;

namespace Application.CashMovement.Commands;

public record CreateCashMovementCommand(CreateCashMovementRequest Request) : IRequest<CashMovementResponse>;
