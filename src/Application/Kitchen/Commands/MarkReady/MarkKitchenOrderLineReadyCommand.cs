using MediatR;

namespace Application.Kitchen.Commands.MarkReady;

public record MarkKitchenOrderLineReadyCommand(int OrderLineId) : IRequest;