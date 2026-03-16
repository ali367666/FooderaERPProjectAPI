using Application.Company.Dtos.Request;
using Application.Company.Dtos.Responce;
using MediatR;

namespace Application.Company.Commands.Create;


public record CreateCompanyCommand(CreateCompanyRequest dto)
    : IRequest<CreateCompanyResponse>;
