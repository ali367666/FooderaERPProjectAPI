using Application.Common.Responce;
using Application.StockMovements.Dtos.Response;
using MediatR;

namespace Application.StockMovements.Queries.SearchStockMovements;

public record SearchStockMovementsQuery(int CompanyId, string? Search)
    : IRequest<BaseResponse<List<StockMovementListItemResponse>>>;
