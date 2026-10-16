using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using MediatR;

namespace Application.MenuItems.Queries.GetStockInfo;

public class MenuItemStockInfoResponse
{
    public int? StockItemId { get; set; }
    public string? StockItemName { get; set; }
    public List<WarehouseQuantityLine> DirectBalances { get; set; } = new();
    public List<WarehouseQuantityLine> RecipeMakeablePortions { get; set; } = new();
}
