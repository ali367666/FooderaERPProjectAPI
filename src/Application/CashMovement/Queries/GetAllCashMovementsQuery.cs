using Application.CashMovement.Commands;
using Application.CashMovement.Dtos;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using MediatR;

namespace Application.CashMovement.Queries;

public record GetAllCashMovementsQuery(int RestaurantId, DateTime From, DateTime To) : IRequest<List<CashMovementResponse>>;
