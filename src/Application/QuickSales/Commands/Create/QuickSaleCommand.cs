using Application.QuickSales.Dtos;
using MediatR;

namespace Application.QuickSales.Commands.Create;

public record QuickSaleCommand(QuickSaleRequest Request) : IRequest<QuickSaleResponse>;
