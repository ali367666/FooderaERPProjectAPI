using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IWarehouseTransferRepository
{
    Task AddAsync(Domain.Entities.WarehouseTransfer warehouseTransfer, CancellationToken cancellationToken);
    Task<Domain.Entities.WarehouseTransfer?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Domain.Entities.WarehouseTransfer?> GetByIdWithLinesAsync(int id, CancellationToken cancellationToken);
    Task<List<Domain.Entities.WarehouseTransfer>> GetAllWithDetailsAsync(CancellationToken cancellationToken);
    void Update(Domain.Entities.WarehouseTransfer warehouseTransfer);
    Task DeleteAsync(Domain.Entities.WarehouseTransfer warehouseTransfer, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}