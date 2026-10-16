namespace Application.Common.Interfaces.Abstracts.Services;

public sealed record PartPaymentResult(
    Domain.Entities.OrderPayment Payment,
    bool OrderClosed,
    decimal RemainingAmount);
