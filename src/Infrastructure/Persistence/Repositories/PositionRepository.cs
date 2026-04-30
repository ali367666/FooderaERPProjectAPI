using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class PositionRepository : IPositionRepository
{
    private readonly AppDbContext _context;

    public PositionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Position position, CancellationToken cancellationToken)
    {
        await _context.Positions.AddAsync(position, cancellationToken);
    }

    /// <inheritdoc cref="IPositionRepository.GetByIdAsync(int, CancellationToken)" />
    public async Task<Position?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Positions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Position?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken)
    {
        return await _context.Positions
            .IgnoreQueryFilters()
            .Include(x => x.Department)
            .Include(x => x.Company)
            .FirstOrDefaultAsync(
                x => x.Id == id && x.CompanyId == companyId,
                cancellationToken);
    }

    public async Task<List<Position>> GetAllAsync(int companyId, CancellationToken cancellationToken)
    {
        return await _context.Positions
            .AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.Company)
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(
        int companyId,
        int departmentId,
        string name,
        CancellationToken cancellationToken)
    {
        var trimmedName = name.Trim().ToLower();

        return await _context.Positions
            .AnyAsync(
                x => x.CompanyId == companyId
                     && x.DepartmentId == departmentId
                     && x.Name.ToLower() == trimmedName,
                cancellationToken);
    }

    public async Task<bool> DepartmentExistsAsync(
        int departmentId,
        int companyId,
        CancellationToken cancellationToken)
    {
        return await _context.Departments
            .AnyAsync(
                x => x.Id == departmentId && x.CompanyId == companyId,
                cancellationToken);
    }

    public void Update(Position position)
    {
        _context.Positions.Update(position);
    }

    public void Delete(Position position)
    {
        _context.Positions.Remove(position);
    }

    public async Task<bool> HasAnyEmployeeAsync(int positionId, CancellationToken cancellationToken)
    {
        return await _context.Employees
            .AnyAsync(x => x.PositionId == positionId, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}