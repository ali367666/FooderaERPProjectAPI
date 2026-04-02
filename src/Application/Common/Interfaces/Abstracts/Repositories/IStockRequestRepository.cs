using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IStockRequestRepository
{
    Task AddAsync(Domain.Entities.StockRequest stockRequest, CancellationToken cancellationToken);
    Task<Domain.Entities.StockRequest?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Domain.Entities.StockRequest?> GetByIdWithLinesAsync(int id, CancellationToken cancellationToken);
    Task<List<Domain.Entities.StockRequest>> GetAllByCompanyIdAsync(int companyId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.StockRequest>> GetByStatusAsync(int companyId, StockRequestStatus status, CancellationToken cancellationToken);
    void Update(Domain.Entities.StockRequest stockRequest);
}