using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Counterparty.Commands;
using Application.Counterparty.Dtos;
using MediatR;

namespace Application.Counterparty.Queries;

public record GetAllCounterpartiesQuery : IRequest<List<CounterpartyResponse>>;
