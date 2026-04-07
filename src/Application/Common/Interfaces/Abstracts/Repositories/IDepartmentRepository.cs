using Domain.Entities;

namespace Application.Abstractions.Repositories;

public interface IDepartmentRepository
{
    Task AddAsync(Department department, CancellationToken cancellationToken);
    Task<Department?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken);
    Task<List<Department>> GetAllAsync(int companyId, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(int companyId, string name, CancellationToken cancellationToken);
    void Update(Department department);
    void Delete(Department department);
    Task<bool> HasAnyPositionAsync(int departmentId, CancellationToken cancellationToken);
    Task<bool> HasAnyEmployeeAsync(int departmentId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}