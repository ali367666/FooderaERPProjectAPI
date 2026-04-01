using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IWarehouseTransferRepository
{
    Task AddAsync(WarehouseTransfer warehouseTransfer, CancellationToken cancellationToken);
    Task<WarehouseTransfer?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<WarehouseTransfer?> GetByIdWithLinesAsync(int id, CancellationToken cancellationToken);
    Task<List<WarehouseTransfer>> GetAllByCompanyIdAsync(int companyId, CancellationToken cancellationToken);
    Task<List<WarehouseTransfer>> GetByStatusAsync(int companyId, TransferStatus status, CancellationToken cancellationToken);
    void Update(WarehouseTransfer warehouseTransfer);
}