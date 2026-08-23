using Application.Common.Responce;
using Application.Company.Dtos.Responce;
using MediatR;

namespace Application.Company.Queries;

public record GetCompanyByCodeQuery(string CompanyCode)
    : IRequest<BaseResponse<GetCompanyByCodeResponse>>;
