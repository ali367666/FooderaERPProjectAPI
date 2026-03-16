using Application.Company.Dtos.Responce;
using MediatR;

namespace Application.Company.Commands.Delete;

public record DeleteCompanyCommand(int Id) : IRequest<DeleteCompanyResponce>;
