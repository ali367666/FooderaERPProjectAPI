using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IStockRequestLineRepository
{
    void Remove(Domain.Entities.StockRequestLine entity);
}