using Application.BackOffice;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class PageCardsRepository : IPageCardsRepository
{
    private readonly AppDbContext _db;

    public PageCardsRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<WarehouseDocumentCards> WarehouseDocumentsAsync(int companyId, CancellationToken ct)
    {
        var documents = _db.WarehouseStockDocuments.AsNoTracking().Where(d => d.CompanyId == companyId);
        var statuses = await documents.GroupBy(d => d.Status).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);
        var lines = await documents
            .Where(d => d.Status == WarehouseStockDocumentStatus.Approved)
            .SelectMany(d => d.Lines)
            .Select(l => new { l.StockItemId, l.Quantity })
            .ToListAsync(ct);

        int Of(WarehouseStockDocumentStatus status) => statuses.Where(s => s.Key == status).Sum(s => s.Count);
        return new WarehouseDocumentCards(
            statuses.Sum(s => s.Count),
            Of(WarehouseStockDocumentStatus.Approved),
            Of(WarehouseStockDocumentStatus.Draft),
            lines.Sum(l => l.Quantity),
            lines.Select(l => l.StockItemId).Distinct().Count());
    }

    public async Task<WarehouseCards> WarehousesAsync(int companyId, CancellationToken ct)
    {
        var rows = await _db.Warehouses.AsNoTracking()
            .Where(w => w.CompanyId == companyId)
            .Select(w => w.IsActive)
            .ToListAsync(ct);
        return new WarehouseCards(rows.Count, rows.Count(active => active));
    }

    public async Task<StockItemCards> StockItemsAsync(int companyId, CancellationToken ct)
    {
        var rows = await _db.StockItems.AsNoTracking()
            .Where(i => i.CompanyId == companyId)
            .Select(i => new { i.IsActive, i.Unit, i.MinimumStock, Quantity = i.WarehouseStocks.Sum(s => s.Quantity) })
            .ToListAsync(ct);
        return new StockItemCards(
            rows.Count,
            rows.Count(i => i.IsActive),
            rows.Count(i => i.IsActive && i.MinimumStock > 0 && i.Quantity < i.MinimumStock),
            rows.Select(i => i.Unit).Distinct().Count());
    }

    public async Task<StockCategoryCards> StockCategoriesAsync(int companyId, CancellationToken ct)
    {
        var categories = _db.Categories.AsNoTracking().Where(c => c.CompanyId == companyId);
        return new StockCategoryCards(
            await categories.CountAsync(ct),
            await categories.CountAsync(c => c.IsActive, ct),
            await _db.StockItems.CountAsync(i => i.CompanyId == companyId, ct));
    }

    public async Task<EmployeeCards> EmployeesAsync(int companyId, DateTime today, CancellationToken ct)
    {
        var employees = _db.Employees.AsNoTracking().Where(e => e.CompanyId == companyId);
        var onLeave = await _db.EmployeeLeaves
            .Where(l => l.CompanyId == companyId && l.FromDate <= today && l.ToDate >= today && l.Employee.IsActive)
            .Select(l => l.EmployeeId)
            .Distinct()
            .CountAsync(ct);
        return new EmployeeCards(await employees.CountAsync(ct), await employees.CountAsync(e => e.IsActive, ct), null, onLeave);
    }

    public async Task<DepartmentCards> DepartmentsAsync(int companyId, CancellationToken ct)
    {
        var departments = _db.Departments.AsNoTracking().Where(d => d.CompanyId == companyId);
        return new DepartmentCards(
            await departments.CountAsync(ct),
            await departments.CountAsync(d => d.IsActive, ct),
            await _db.Employees.CountAsync(e => e.CompanyId == companyId && e.IsActive, ct));
    }

    public async Task<Dictionary<int, decimal>> StockQuantitiesAsync(IReadOnlyCollection<int> stockItemIds, CancellationToken ct)
        => await _db.WarehouseStocks.AsNoTracking()
            .Where(s => stockItemIds.Contains(s.StockItemId))
            .GroupBy(s => s.StockItemId)
            .Select(g => new { g.Key, Quantity = g.Sum(s => s.Quantity) })
            .ToDictionaryAsync(x => x.Key, x => x.Quantity, ct);

    public async Task<Dictionary<int, int>> ItemCountsByCategoryAsync(IReadOnlyCollection<int> categoryIds, CancellationToken ct)
        => await _db.StockItems.AsNoTracking()
            .Where(i => categoryIds.Contains(i.CategoryId))
            .GroupBy(i => i.CategoryId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

    public async Task<Dictionary<int, int>> EmployeeCountsByDepartmentAsync(IReadOnlyCollection<int> departmentIds, CancellationToken ct)
        => await _db.Employees.AsNoTracking()
            .Where(e => e.IsActive && departmentIds.Contains(e.DepartmentId))
            .GroupBy(e => e.DepartmentId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

    public async Task<Dictionary<int, string?>> EmployeePhotosAsync(IReadOnlyCollection<int> employeeIds, CancellationToken ct)
        => await _db.Employees.AsNoTracking()
            .Where(e => employeeIds.Contains(e.Id) && e.User != null)
            .Select(e => new { e.Id, e.User!.AvatarUrl })
            .ToDictionaryAsync(x => x.Id, x => x.AvatarUrl, ct);

    public async Task<HashSet<int>> EmployeesOnLeaveAsync(IReadOnlyCollection<int> employeeIds, DateTime today, CancellationToken ct)
        => (await _db.EmployeeLeaves.AsNoTracking()
                .Where(l => employeeIds.Contains(l.EmployeeId) && l.FromDate <= today && l.ToDate >= today)
                .Select(l => l.EmployeeId)
                .ToListAsync(ct))
            .ToHashSet();
}
