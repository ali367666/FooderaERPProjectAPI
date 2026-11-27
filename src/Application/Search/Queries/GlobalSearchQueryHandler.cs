using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Search.Dtos;
using Domain.Constants;
using MediatR;

namespace Application.Search.Queries;

public class GlobalSearchQueryHandler : IRequestHandler<GlobalSearchQuery, List<GlobalSearchResult>>
{
    private const int MaxPerKind = 5;
    private const int MinLength = 2;

    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IMenuCategoryRepository _menuCategoryRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICounterpartyRepository _counterpartyRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IRestaurantTableRepository _tableRepository;
    private readonly ICurrentUserService _currentUserService;

    public GlobalSearchQueryHandler(
        IMenuItemRepository menuItemRepository,
        IMenuCategoryRepository menuCategoryRepository,
        IEmployeeRepository employeeRepository,
        ICounterpartyRepository counterpartyRepository,
        IRestaurantRepository restaurantRepository,
        IRestaurantTableRepository tableRepository,
        ICurrentUserService currentUserService)
    {
        _menuItemRepository = menuItemRepository;
        _menuCategoryRepository = menuCategoryRepository;
        _employeeRepository = employeeRepository;
        _counterpartyRepository = counterpartyRepository;
        _restaurantRepository = restaurantRepository;
        _tableRepository = tableRepository;
        _currentUserService = currentUserService;
    }

    public async Task<List<GlobalSearchResult>> Handle(GlobalSearchQuery request, CancellationToken cancellationToken)
    {
        var words = SearchText.Fold(request.Term)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length == 0 || words.Sum(w => w.Length) < MinLength)
            return new List<GlobalSearchResult>();

        // A tenant is always confined to its own company; the platform SuperAdmin may look into another.
        var companyId = _currentUserService.IsSuperAdmin && request.CompanyId is > 0
            ? request.CompanyId.Value
            : _currentUserService.CompanyId;

        bool Matches(params string?[] parts) =>
            words.All(w => SearchText.Fold(string.Join(' ', parts.Where(p => !string.IsNullOrWhiteSpace(p)))).Contains(w, StringComparison.Ordinal));

        var results = new List<GlobalSearchResult>();

        if (_currentUserService.HasPermission(AppPermissions.MenuItemView))
        {
            var items = await _menuItemRepository.GetAllAsync(companyId, cancellationToken);
            results.AddRange(items
                .Where(i => Matches(i.Name, i.Barcode, i.MenuCategory?.Name))
                .Take(MaxPerKind)
                .Select(i => new GlobalSearchResult
                {
                    Kind = "Məhsul",
                    Title = i.Name,
                    Subtitle = string.Join(" · ", new[] { i.MenuCategory?.Name, i.IsActive ? null : "Passiv" }.Where(s => !string.IsNullOrEmpty(s))),
                    Href = "/dashboard/menu-items"
                }));
        }

        if (_currentUserService.HasPermission(AppPermissions.MenuCategoryView))
        {
            var categories = await _menuCategoryRepository.GetAllAsync(companyId, cancellationToken);
            results.AddRange(categories
                .Where(c => Matches(c.Name))
                .Take(MaxPerKind)
                .Select(c => new GlobalSearchResult
                {
                    Kind = "Kateqoriya",
                    Title = c.Name,
                    Href = "/dashboard/menu-categories"
                }));
        }

        if (_currentUserService.HasPermission(AppPermissions.EmployeeView))
        {
            var employees = await _employeeRepository.GetAllAsync(companyId, cancellationToken);
            results.AddRange(employees
                .Where(e => Matches(e.FirstName, e.LastName, e.Email, e.PhoneNumber))
                .Take(MaxPerKind)
                .Select(e => new GlobalSearchResult
                {
                    Kind = "İşçi",
                    Title = $"{e.FirstName} {e.LastName}".Trim(),
                    Subtitle = string.Join(" · ", new[] { e.Position?.Name, e.Department?.Name }.Where(s => !string.IsNullOrEmpty(s))),
                    Href = "/dashboard/employees"
                }));
        }

        if (_currentUserService.HasPermission(AppPermissions.CounterpartyView))
        {
            var counterparties = await _counterpartyRepository.GetAllByCompanyAsync(companyId, cancellationToken);
            results.AddRange(counterparties
                .Where(c => Matches(c.Name, c.PhoneNumber))
                .Take(MaxPerKind)
                .Select(c => new GlobalSearchResult
                {
                    Kind = "Konturagent",
                    Title = c.Name,
                    Subtitle = string.Join(" · ", new[] { c.Category?.Name, c.IsActive ? null : "Passiv" }.Where(s => !string.IsNullOrEmpty(s))),
                    Href = "/dashboard/counterparties"
                }));
        }

        if (_currentUserService.HasPermission(AppPermissions.RestaurantView))
        {
            var restaurants = await _restaurantRepository.GetByCompanyIdAsync(companyId, cancellationToken);
            results.AddRange(restaurants
                .Where(r => Matches(r.Name, r.Address, r.Phone))
                .Take(MaxPerKind)
                .Select(r => new GlobalSearchResult
                {
                    Kind = "Filial",
                    Title = r.Name,
                    Subtitle = r.Address,
                    Href = "/dashboard/restaurants"
                }));
        }

        if (_currentUserService.HasPermission(AppPermissions.RestaurantTableView))
        {
            var tables = await _tableRepository.GetAllAsync(companyId, cancellationToken);
            results.AddRange(tables
                .Where(t => Matches(t.Name, t.Note))
                .Take(MaxPerKind)
                .Select(t => new GlobalSearchResult
                {
                    Kind = "Masa",
                    Title = t.Name,
                    Subtitle = t.Restaurant?.Name,
                    Href = "/dashboard/restaurant-tables"
                }));
        }

        return results;
    }
}
