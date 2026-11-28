using Application.BackOffice;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// The number cards above the admin lists, for the caller's company (the SuperAdmin: the company picked in the filter).
/// </summary>
[ApiController]
[Authorize]
public class PageCardsController : ControllerBase
{
    private readonly IPageCardsRepository _cards;
    private readonly ICurrentUserService _currentUser;

    public PageCardsController(IPageCardsRepository cards, ICurrentUserService currentUser)
    {
        _cards = cards;
        _currentUser = currentUser;
    }

    [Authorize(Policy = AppPermissions.WarehouseStockView)]
    [HttpGet("api/WarehouseStock/summary")]
    public async Task<ActionResult<WarehouseDocumentCards>> WarehouseDocuments(CancellationToken ct)
        => Ok(await _cards.WarehouseDocumentsAsync(_currentUser.CompanyId, ct));

    [Authorize(Policy = AppPermissions.WarehouseView)]
    [HttpGet("api/Warehouses/summary")]
    public async Task<ActionResult<WarehouseCards>> Warehouses(CancellationToken ct)
        => Ok(await _cards.WarehousesAsync(_currentUser.CompanyId, ct));

    [Authorize(Policy = AppPermissions.StockItemView)]
    [HttpGet("api/StockItem/summary")]
    public async Task<ActionResult<StockItemCards>> StockItems(CancellationToken ct)
        => Ok(await _cards.StockItemsAsync(_currentUser.CompanyId, ct));

    [Authorize(Policy = AppPermissions.StockCategoryView)]
    [HttpGet("api/StockCategory/summary")]
    public async Task<ActionResult<StockCategoryCards>> StockCategories(CancellationToken ct)
        => Ok(await _cards.StockCategoriesAsync(_currentUser.CompanyId, ct));

    [Authorize(Policy = AppPermissions.EmployeeView)]
    [HttpGet("api/Employees/summary")]
    public async Task<ActionResult<EmployeeCards>> Employees(CancellationToken ct)
        => Ok(await _cards.EmployeesAsync(_currentUser.CompanyId, BusinessTime.Now.Date, ct));

    [Authorize(Policy = AppPermissions.DepartmentView)]
    [HttpGet("api/Departments/summary")]
    public async Task<ActionResult<DepartmentCards>> Departments(CancellationToken ct)
        => Ok(await _cards.DepartmentsAsync(_currentUser.CompanyId, ct));
}
