using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using MediatR;

namespace Application.MenuItems.Queries.GetStockInfo;

public class WarehouseQuantityLine
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = default!;
    public decimal Quantity { get; set; }
}
