using MediatR;

namespace Application.Orders.Commands.SetServiceCharge;

/// <summary>
/// "Servis haqqı qeyd etmək" — records the table's service charge in manat (0 clears it). It is added
/// to the payment that closes the order. Returns the amount now on the order.
/// </summary>
public record SetOrderServiceChargeCommand(int OrderId, decimal Amount) : IRequest<decimal>;
