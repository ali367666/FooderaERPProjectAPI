using Application.CounterpartyCategory.Dtos;
using MediatR;

namespace Application.CounterpartyCategory.Commands;

public record DeleteCounterpartyCategoryCommand(int Id) : IRequest;
