using Domain.Entities.WarehouseAndStock;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IStockRequestLineRepository
{
    void Remove(StockRequestLine entity);
}