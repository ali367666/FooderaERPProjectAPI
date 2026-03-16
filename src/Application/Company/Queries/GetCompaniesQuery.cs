using Application.Company.Dtos.Responce;
using MediatR;

namespace Application.Company.Queries;

public record GetCompaniesQuery : IRequest<List<GetAllCompaniesResponse>>;
