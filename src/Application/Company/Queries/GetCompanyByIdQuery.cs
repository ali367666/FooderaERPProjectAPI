using Application.Common.Responce;
using Application.Company.Dtos.Responce;
using MediatR;

namespace Application.Company.Queries;


public record GetCompanyByIdQuery(int Id)
    : IRequest<BaseResponse<GetCompanyByIdResponse>>;