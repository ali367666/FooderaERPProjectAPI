using Application.Counterparty.Dtos;
using MediatR;

namespace Application.Counterparty.Commands;

public record DeleteCounterpartyCommand(int Id) : IRequest;
