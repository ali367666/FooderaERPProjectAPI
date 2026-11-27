using Application.Counterparty.Dtos;
using MediatR;

namespace Application.Counterparty.Queries;

/// <summary>The active fixed banks the POS offers when a card payment is taken.</summary>
public record GetPaymentBanksQuery : IRequest<List<PaymentBankResponse>>;
