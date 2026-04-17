using Domain.Enums;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IStockRequestRepository
{
    Task AddAsync(Domain.Entities.WarehouseAndStock.StockRequest stockRequest, CancellationToken cancellationToken);
    Task<Domain.Entities.WarehouseAndStock.StockRequest?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Domain.Entities.WarehouseAndStock.StockRequest?> GetByIdWithLinesAsync(int id, CancellationToken cancellationToken);
    Task<List<Domain.Entities.WarehouseAndStock.StockRequest>> GetAllByCompanyIdAsync(int companyId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.WarehouseAndStock.StockRequest>> GetByStatusAsync(int companyId, StockRequestStatus status, CancellationToken cancellationToken);
    void Update(Domain.Entities.WarehouseAndStock.StockRequest stockRequest);
    
    Task<List<Domain.Entities.WarehouseAndStock.StockRequest>> GetAllWithDetailsAsync(CancellationToken cancellationToken);
    Task DeleteAsync(Domain.Entities.WarehouseAndStock.StockRequest stockRequest, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<Domain.Entities.WarehouseAndStock.StockRequest?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken);
}