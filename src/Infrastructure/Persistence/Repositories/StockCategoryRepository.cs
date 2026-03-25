using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class StockCategoryRepository : IStockCategoryRepository
{
    private readonly AppDbContext _context;

    public StockCategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<StockCategory?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Categories
            .Include(x => x.Company)
            .Include(x => x.Parent)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<StockCategory?> GetByIdWithChildrenAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Categories
            .Include(x => x.Company)
            .Include(x => x.Parent)
            .Include(x => x.Children)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<StockCategory>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Categories
            .Include(x => x.Company)
            .Include(x => x.Parent)
            .Include(x => x.Children)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<StockCategory>> GetAllByCompanyIdAsync(int companyId, CancellationToken cancellationToken)
    {
        return await _context.Categories
            .Where(x => x.CompanyId == companyId)
            .Include(x => x.Parent)
            .Include(x => x.Children)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<StockCategory>> GetActiveByCompanyIdAsync(int companyId, CancellationToken cancellationToken)
    {
        return await _context.Categories
            .Where(x => x.CompanyId == companyId && x.IsActive)
            .Include(x => x.Parent)
            .Include(x => x.Children)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<StockCategory>> GetRootCategoriesByCompanyIdAsync(int companyId, CancellationToken cancellationToken)
    {
        return await _context.Categories
            .Where(x => x.CompanyId == companyId && x.ParentId == null)
            .Include(x => x.Children)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<StockCategory>> GetChildrenByParentIdAsync(int parentId, CancellationToken cancellationToken)
    {
        return await _context.Categories
            .Where(x => x.ParentId == parentId)
            .Include(x => x.Parent)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Categories
            .AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, int companyId, CancellationToken cancellationToken)
    {
        return await _context.Categories
            .AnyAsync(x => x.CompanyId == companyId && x.Name.ToLower() == name.ToLower(), cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, int companyId, int excludeId, CancellationToken cancellationToken)
    {
        return await _context.Categories
            .AnyAsync(x =>
                x.CompanyId == companyId &&
                x.Id != excludeId &&
                x.Name.ToLower() == name.ToLower(),
                cancellationToken);
    }

    public async Task<bool> BelongsToCompanyAsync(int categoryId, int companyId, CancellationToken cancellationToken)
    {
        return await _context.Categories
            .AnyAsync(x => x.Id == categoryId && x.CompanyId == companyId, cancellationToken);
    }

    public async Task<bool> HasChildrenAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Categories
            .AnyAsync(x => x.ParentId == id, cancellationToken);
    }

    public async Task AddAsync(StockCategory stockCategory, CancellationToken cancellationToken)
    {
        await _context.Categories.AddAsync(stockCategory, cancellationToken);
    }

    public void Update(StockCategory stockCategory)
    {
        _context.Categories.Update(stockCategory);
    }

    public void Delete(StockCategory stockCategory)
    {
        _context.Categories.Remove(stockCategory);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}