using Domain.Entities;
using System.Linq.Expressions;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IPositionRepository
{
    Task AddAsync(Domain.Entities.Position position, CancellationToken cancellationToken);
    Task<Domain.Entities.Position?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.Position>> GetAllAsync(int companyId, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(int companyId, int departmentId, string name, CancellationToken cancellationToken);
    void Update(Domain.Entities.Position position);
    void Delete(Domain.Entities.Position position);
    Task<bool> HasAnyEmployeeAsync(int positionId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<bool> DepartmentExistsAsync(int departmentId, int companyId, CancellationToken cancellationToken);
}