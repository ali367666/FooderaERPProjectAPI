using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _context;

    public EmployeeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Employee employee, CancellationToken cancellationToken)
    {
        await _context.Employees.AddAsync(employee, cancellationToken);
    }

    public async Task<Employee?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken)
    {
        return await _context.Employees
            .Include(x => x.Department)
            .Include(x => x.Position)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);
    }

    public async Task<List<Employee>> GetAllAsync(int companyId, CancellationToken cancellationToken)
    {
        return await _context.Employees
            .Include(x => x.Department)
            .Include(x => x.Position)
            .Include(x => x.User)
            .Where(x => x.CompanyId == companyId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Employee>> GetByPositionAsync(
        int companyId,
        int? positionId,
        string? positionName,
        CancellationToken cancellationToken)
    {
        var query = _context.Employees
            .Include(x => x.Department)
            .Include(x => x.Position)
            .Include(x => x.User)
            .Where(x => x.CompanyId == companyId);

        if (positionId.HasValue && positionId.Value > 0)
        {
            query = query.Where(x => x.PositionId == positionId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(positionName))
        {
            var normalizedPositionName = positionName.Trim().ToLower();
            query = query.Where(x =>
                x.Position != null &&
                x.Position.Name != null &&
                x.Position.Name.Trim().ToLower() == normalizedPositionName);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        return await _context.Employees
            .AnyAsync(x => x.UserId == userId, cancellationToken);
    }

    public void Update(Employee employee)
    {
        _context.Employees.Update(employee);
    }

    public void Delete(Employee employee)
    {
        _context.Employees.Remove(employee);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}