using Application.Company.Dtos.Request;
using MediatR;

namespace Application.Company.Commands.Create;

public record CreateCompanyCommand(CreateCompanyRequest dto) : IRequest<int>;
