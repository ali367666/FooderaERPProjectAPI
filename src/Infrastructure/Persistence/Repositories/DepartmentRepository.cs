using Application.Abstractions.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly AppDbContext _context;

    public DepartmentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Department department, CancellationToken cancellationToken)
    {
        await _context.Departments.AddAsync(department, cancellationToken);
    }

    public async Task<Department?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken)
    {
        return await _context.Departments
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
    }

    public async Task<List<Department>> GetAllAsync(int companyId, CancellationToken cancellationToken)
    {
        return await _context.Departments
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(int companyId, string name, CancellationToken cancellationToken)
    {
        return await _context.Departments
            .AnyAsync(x => x.CompanyId == companyId && x.Name == name, cancellationToken);
    }

    public void Update(Department department)
    {
        _context.Departments.Update(department);
    }

    public void Delete(Department department)
    {
        _context.Departments.Remove(department);
    }

    public async Task<bool> HasAnyPositionAsync(int departmentId, CancellationToken cancellationToken)
    {
        return await _context.Positions
            .AnyAsync(x => x.DepartmentId == departmentId, cancellationToken);
    }

    public async Task<bool> HasAnyEmployeeAsync(int departmentId, CancellationToken cancellationToken)
    {
        return await _context.Employees
            .AnyAsync(x => x.DepartmentId == departmentId, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}