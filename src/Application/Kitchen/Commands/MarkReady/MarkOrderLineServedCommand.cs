using MediatR;

namespace Application.OrderLines.Commands.MarkServed;

public record MarkOrderLineServedCommand(int OrderLineId) : IRequest;