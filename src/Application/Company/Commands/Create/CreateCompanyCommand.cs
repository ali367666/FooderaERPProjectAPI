using Application.Common.Responce;
using Application.Company.Dtos.Request;
using Application.Company.Dtos.Responce;
using MediatR;

namespace Application.Company.Commands.Create;


public record CreateCompanyCommand(CreateCompanyRequest Request)
    : IRequest<BaseResponse<CreateCompanyResponse>>;
