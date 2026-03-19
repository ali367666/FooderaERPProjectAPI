using Application.Common.Responce;
using Application.Company.Dtos.Request;
using Application.Company.Dtos.Responce;
using MediatR;

namespace Application.Company.Commands.Update;

public record UpdateCompanyCommand(int Id, UpdateCompanyRequest dto) : IRequest<BaseResponse<UpdateCompanyResponse>>;