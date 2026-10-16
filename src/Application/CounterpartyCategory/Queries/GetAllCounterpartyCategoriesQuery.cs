using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.CounterpartyCategory.Commands;
using Application.CounterpartyCategory.Dtos;
using MediatR;

namespace Application.CounterpartyCategory.Queries;

public record GetAllCounterpartyCategoriesQuery : IRequest<List<CounterpartyCategoryResponse>>;
