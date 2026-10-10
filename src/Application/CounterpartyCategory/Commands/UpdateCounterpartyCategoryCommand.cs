using Application.CounterpartyCategory.Dtos;
using MediatR;

namespace Application.CounterpartyCategory.Commands;

public record UpdateCounterpartyCategoryCommand(UpdateCounterpartyCategoryRequest Request) : IRequest<CounterpartyCategoryResponse>;
