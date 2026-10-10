using Application.CounterpartyCategory.Dtos;
using MediatR;

namespace Application.CounterpartyCategory.Commands;

public record CreateCounterpartyCategoryCommand(CreateCounterpartyCategoryRequest Request) : IRequest<CounterpartyCategoryResponse>;
