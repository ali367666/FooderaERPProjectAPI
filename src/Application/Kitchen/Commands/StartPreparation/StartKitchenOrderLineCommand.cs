using MediatR;

namespace Application.Kitchen.Commands.StartPreparation;

public record StartKitchenOrderLineCommand(int OrderLineId) : IRequest;