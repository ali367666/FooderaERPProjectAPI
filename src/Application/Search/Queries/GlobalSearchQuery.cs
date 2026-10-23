using Application.Search.Dtos;
using MediatR;

namespace Application.Search.Queries;

public record GlobalSearchQuery(string? Term, int? CompanyId = null) : IRequest<List<GlobalSearchResult>>;
