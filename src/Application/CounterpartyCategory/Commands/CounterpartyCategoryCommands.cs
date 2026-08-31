using Application.CounterpartyCategory.Dtos;
using MediatR;

namespace Application.CounterpartyCategory.Commands;

public record CreateCounterpartyCategoryCommand(CreateCounterpartyCategoryRequest Request) : IRequest<CounterpartyCategoryResponse>;

public record UpdateCounterpartyCategoryCommand(UpdateCounterpartyCategoryRequest Request) : IRequest<CounterpartyCategoryResponse>;

public record DeleteCounterpartyCategoryCommand(int Id) : IRequest;
