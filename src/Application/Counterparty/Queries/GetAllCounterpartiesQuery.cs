using Application.Counterparty.Dtos;
using MediatR;

namespace Application.Counterparty.Queries;

/// <summary>
/// <paramref name="Status"/>: "active" (the default, what every picker should offer), "passive" or "all".
/// </summary>
public record GetAllCounterpartiesQuery(string? Status = null) : IRequest<List<CounterpartyResponse>>;
