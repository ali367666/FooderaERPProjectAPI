namespace Application.Common.Interfaces.Abstracts.Services;

/// <param name="OrderLineId">The order line being paid for.</param>
/// <param name="Quantity">How much of it, in the line's own unit.</param>
public sealed record PartPaymentLine(int OrderLineId, int Quantity);
