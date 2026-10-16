using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Domain.Enums;
using MediatR;

namespace Application.SaleReturn;

public record CreateSaleReturnCommand(CreateSaleReturnRequest Request) : IRequest<SaleReturnResponse>;
