using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IStockRequestRepository
{
    Task AddAsync(StockRequest stockRequest, CancellationToken cancellationToken);
    Task<StockRequest?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<StockRequest?> GetByIdWithLinesAsync(int id, CancellationToken cancellationToken);
    Task<List<StockRequest>> GetAllByCompanyIdAsync(int companyId, CancellationToken cancellationToken);
    Task<List<StockRequest>> GetByStatusAsync(int companyId, StockRequestStatus status, CancellationToken cancellationToken);
    void Update(StockRequest stockRequest);
}