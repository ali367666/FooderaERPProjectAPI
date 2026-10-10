using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Shift.Dtos;
using MediatR;

namespace Application.Shift.Queries;

public record GetCurrentShiftQuery(int RestaurantId) : IRequest<ShiftResponse?>;
