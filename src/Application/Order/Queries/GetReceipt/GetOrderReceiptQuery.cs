using Application.Orders.Dtos;
using MediatR;

namespace Application.Orders.Queries.GetReceipt;

/// <param name="PaymentId">When set, the receipt covers only that part payment (one guest's share).</param>
/// <param name="Fiscal">For a bill not yet paid (pre-check): print it as a fiscal or ordinary receipt. Paid orders use the stored choice.</param>
public record GetOrderReceiptQuery(int OrderId, int? PaymentId = null, bool? Fiscal = null) : IRequest<OrderReceiptResponse>;
