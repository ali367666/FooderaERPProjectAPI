using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.StockMovements.Dtos.Response;
using Domain.Enums;
using MediatR;

namespace Application.StockMovements.Queries.SearchStockMovements;

public class SearchStockMovementsQueryHandler
    : IRequestHandler<SearchStockMovementsQuery, BaseResponse<List<StockMovementListItemResponse>>>
{
    private readonly IStockMovementRepository _stockMovementRepository;

    public SearchStockMovementsQueryHandler(IStockMovementRepository stockMovementRepository)
    {
        _stockMovementRepository = stockMovementRepository;
    }

    public async Task<BaseResponse<List<StockMovementListItemResponse>>> Handle(
        SearchStockMovementsQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await _stockMovementRepository.SearchByCompanyAsync(
            request.CompanyId,
            request.Search,
            cancellationToken);

        var data = rows.Select(x => new StockMovementListItemResponse
        {
            CompanyId = x.CompanyId,
            Id = x.Id,
            SourceDocumentNo = x.SourceDocumentNo,
            StockItemId = x.StockItemId,
            StockItemName = x.StockItem.Name,
            StockItemUnit = x.StockItem.Unit,
            WarehouseName = x.Warehouse.Name,
            FromWarehouseName = x.FromWarehouse?.Name,
            ToWarehouseName = x.ToWarehouse?.Name,
            Quantity = x.Quantity,
            MovementType = MovementTypeLabel(x.Type),
            SourceType = SourceTypeLabel(x.SourceType),
            MovementDate = x.MovementDate,
            Note = x.Note
        }).ToList();

        return BaseResponse<List<StockMovementListItemResponse>>.Ok(data);
    }

    private static string MovementTypeLabel(StockMovementType t) => t switch
    {
        StockMovementType.TransferOut => "Transfer out",
        StockMovementType.TransferIn => "Transfer in",
        StockMovementType.StockEntryIn => "Stock entry (in)",
        _ => t.ToString()
    };

    private static string SourceTypeLabel(StockMovementSourceType t) => t switch
    {
        StockMovementSourceType.WarehouseTransfer => "Warehouse transfer",
        StockMovementSourceType.StockEntryDocument => "Stock entry document",
        _ => t.ToString()
    };
}
