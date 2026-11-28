namespace Application.BackOffice;

public record WarehouseDocumentCards(int Total, int Approved, int Draft, decimal ReceivedQuantity, int ReceivedProductCount);

public record WarehouseCards(int Total, int Active);

public record StockItemCards(int Total, int Active, int Critical, int UnitCount);

public record StockCategoryCards(int Total, int Active, int ProductCount);

/// <param name="OnShift">Null: the system does not record which employee is at work.</param>
public record EmployeeCards(int Total, int Active, int? OnShift, int OnLeave);

public record DepartmentCards(int Total, int Active, int EmployeeCount);

public interface IPageCardsRepository
{
    Task<WarehouseDocumentCards> WarehouseDocumentsAsync(int companyId, CancellationToken ct);
    Task<WarehouseCards> WarehousesAsync(int companyId, CancellationToken ct);
    Task<StockItemCards> StockItemsAsync(int companyId, CancellationToken ct);
    Task<StockCategoryCards> StockCategoriesAsync(int companyId, CancellationToken ct);
    Task<EmployeeCards> EmployeesAsync(int companyId, DateTime today, CancellationToken ct);
    Task<DepartmentCards> DepartmentsAsync(int companyId, CancellationToken ct);

    Task<Dictionary<int, decimal>> StockQuantitiesAsync(IReadOnlyCollection<int> stockItemIds, CancellationToken ct);
    Task<Dictionary<int, int>> ItemCountsByCategoryAsync(IReadOnlyCollection<int> categoryIds, CancellationToken ct);
    Task<Dictionary<int, int>> EmployeeCountsByDepartmentAsync(IReadOnlyCollection<int> departmentIds, CancellationToken ct);
    Task<Dictionary<int, string?>> EmployeePhotosAsync(IReadOnlyCollection<int> employeeIds, CancellationToken ct);
    Task<HashSet<int>> EmployeesOnLeaveAsync(IReadOnlyCollection<int> employeeIds, DateTime today, CancellationToken ct);
}
