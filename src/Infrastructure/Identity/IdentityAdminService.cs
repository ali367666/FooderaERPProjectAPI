using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.IdentityAdmin;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Infrastructure.Identity;

public class IdentityAdminService : IIdentityAdminService
{
    /// <summary>Permissions reserved for the platform SuperAdmin — never visible/assignable to a tenant.</summary>
    private static readonly string[] PlatformOnlyPermissions =
    [
        AppPermissions.CompanyView,
        AppPermissions.CompanyCreate,
        AppPermissions.CompanyUpdate,
        AppPermissions.CompanyDelete,
    ];

    private readonly UserManager<User> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly AppDbContext _db;
    private readonly ICompanyRepository _companyRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUserService;

    public IdentityAdminService(
        UserManager<User> userManager,
        RoleManager<AppRole> roleManager,
        AppDbContext db,
        ICompanyRepository companyRepository,
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
        _companyRepository = companyRepository;
        _employeeRepository = employeeRepository;
        _currentUserService = currentUserService;
    }

    private bool IsSuperAdmin => _currentUserService.IsSuperAdmin;

    private async Task<int> AddUserToRoleAsync(User user, AppRole role, CancellationToken cancellationToken)
    {
        var exists = await _db.UserRoles.AnyAsync(
            ur => ur.UserId == user.Id && ur.RoleId == role.Id, cancellationToken);
        if (exists) return 0;
        _db.UserRoles.Add(new IdentityUserRole<int> { UserId = user.Id, RoleId = role.Id });
        return await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<int> RemoveUserFromRoleAsync(User user, AppRole role, CancellationToken cancellationToken)
    {
        var existing = await _db.UserRoles.FirstOrDefaultAsync(
            ur => ur.UserId == user.Id && ur.RoleId == role.Id, cancellationToken);
        if (existing is null) return 0;
        _db.UserRoles.Remove(existing);
        return await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Inserts a role directly, bypassing RoleManager.CreateAsync — ASP.NET Identity's default
    /// RoleValidator enforces a GLOBALLY unique name, which would reject every per-company role
    /// whose name matches another company's (or a global template's) role.
    /// </summary>
    private async Task CreateRoleDirectAsync(AppRole role, CancellationToken cancellationToken)
    {
        _db.Roles.Add(role);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<UserListItemDto>> GetUsersAsync(int? companyIdFilter, CancellationToken cancellationToken = default)
    {
        var q = _db.Users.AsNoTracking().Include(u => u.Company).Include(u => u.Restaurant).Include(u => u.Warehouse).AsQueryable();
        if (companyIdFilter is > 0)
            q = q.Where(u => u.CompanyId == companyIdFilter);

        var users = await q.OrderBy(u => u.Id).ToListAsync(cancellationToken);
        var result = new List<UserListItemDto>();

        foreach (var u in users)
        {
            var trackUser = await _userManager.FindByIdAsync(u.Id.ToString());
            if (trackUser is null) continue;
            var roles = (await _userManager.GetRolesAsync(trackUser)).ToList();
            var emp = await _db.Employees.AsNoTracking()
                .FirstOrDefaultAsync(e => e.UserId == u.Id, cancellationToken);

            result.Add(new UserListItemDto
            {
                Id = u.Id,
                FullName = u.FullName,
                UserName = u.UserName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                IsActive = u.IsActive,
                CompanyId = u.CompanyId,
                CompanyName = u.Company?.Name,
                Roles = roles,
                LinkedEmployeeId = emp?.Id,
                Code = u.Code,
                RfidCardId = u.RfidCardId,
                CanAccessAdminPanel = u.CanAccessAdminPanel,
                CanAccessFrontOffice = u.CanAccessFrontOffice,
                WorkplaceType = u.WorkplaceType,
                RestaurantId = u.RestaurantId,
                RestaurantName = u.Restaurant?.Name,
                WarehouseId = u.WarehouseId,
                WarehouseName = u.Warehouse?.Name
            });
        }

        return result;
    }

    public async Task<UserDetailDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var u = await _db.Users.AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Restaurant)
            .Include(x => x.Warehouse)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (u is null) return null;

        var trackUser = await _userManager.FindByIdAsync(id.ToString());
        if (trackUser is null) return null;
        var roles = (await _userManager.GetRolesAsync(trackUser)).ToList();
        var emp = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == u.Id, cancellationToken);

        return new UserDetailDto
        {
            Id = u.Id,
            FullName = u.FullName,
            UserName = u.UserName,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            IsActive = u.IsActive,
            CompanyId = u.CompanyId,
            CompanyName = u.Company?.Name,
            Roles = roles,
            LinkedEmployeeId = emp?.Id,
            Code = u.Code,
            RfidCardId = u.RfidCardId,
            CanAccessAdminPanel = u.CanAccessAdminPanel,
            CanAccessFrontOffice = u.CanAccessFrontOffice,
            WorkplaceType = u.WorkplaceType,
            RestaurantId = u.RestaurantId,
            RestaurantName = u.Restaurant?.Name,
            WarehouseId = u.WarehouseId,
            WarehouseName = u.Warehouse?.Name
        };
    }

    public async Task<(bool Ok, int? UserId, string? Error, Dictionary<string, string[]>? FieldErrors)> CreateUserAsync(
        CreateUserAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _companyRepository.ExistsAsync(request.CompanyId, cancellationToken))
            return (false, null, "Company was not found.", null);

        var email = request.Email.Trim();
        var userName = request.UserName.Trim();
        var fullName = request.FullName.Trim();
        if (string.IsNullOrEmpty(fullName))
            return (false, null, "Full name is required.",
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { ["FullName"] = new[] { "Full name is required." } });
        if (string.IsNullOrEmpty(userName))
            return (false, null, "Username is required.",
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { ["UserName"] = new[] { "Username is required." } });
        if (string.IsNullOrEmpty(email))
            return (false, null, "Email is required.",
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { ["Email"] = new[] { "Email is required." } });
        if (string.IsNullOrEmpty(request.Password))
            return (false, null, "Password is required.",
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { ["Password"] = new[] { "Password is required." } });

        if (await _userManager.FindByEmailAsync(email) is not null)
            return (false, null, "This email is already in use.",
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { ["Email"] = new[] { "This email is already in use." } });
        if (await _userManager.FindByNameAsync(userName) is not null)
            return (false, null, "This username is already in use.",
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { ["UserName"] = new[] { "This username is already in use." } });

        var code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();
        if (code is not null)
        {
            if (code.Length != 4 || !code.All(char.IsDigit))
                return (false, null, "Code must be exactly 4 digits.",
                    new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { ["Code"] = new[] { "Code must be exactly 4 digits." } });

            var codeTaken = await _db.Users.AsNoTracking()
                .AnyAsync(x => x.CompanyId == request.CompanyId && x.Code == code, cancellationToken);
            if (codeTaken)
                return (false, null, "This code is already in use in this company.",
                    new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { ["Code"] = new[] { "This code is already in use." } });
        }

        if (request.WorkplaceType == EmployeeWorkplaceType.Restaurant)
        {
            if (!request.RestaurantId.HasValue)
                return (false, null, "Restaurant must be selected for a restaurant-scoped user.",
                    new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { ["RestaurantId"] = new[] { "Restaurant is required." } });

            var restaurantExists = await _db.Restaurants.AsNoTracking()
                .AnyAsync(r => r.Id == request.RestaurantId.Value && r.CompanyId == request.CompanyId, cancellationToken);
            if (!restaurantExists)
                return (false, null, "Restaurant was not found for this company.", null);
        }

        Employee? employee = null;
        if (request.EmployeeId is > 0)
        {
            employee = await _employeeRepository.GetByIdAsync(
                request.EmployeeId.Value,
                request.CompanyId,
                cancellationToken);
            if (employee is null)
                return (false, null, "Employee not found for this company.", null);
            if (employee.UserId.HasValue)
                return (false, null, "That employee is already linked to a user account.", null);
        }

        if (request.WarehouseId is > 0)
        {
            var warehouseExists = await _db.Warehouses.AsNoTracking()
                .AnyAsync(w => w.Id == request.WarehouseId.Value && w.CompanyId == request.CompanyId, cancellationToken);
            if (!warehouseExists)
                return (false, null, "Warehouse was not found for this company.", null);
        }

        var user = new User
        {
            UserName = userName,
            Email = email,
            FullName = fullName,
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            IsActive = request.IsActive,
            CompanyId = request.CompanyId,
            WorkplaceType = request.WorkplaceType,
            RestaurantId = request.WorkplaceType == EmployeeWorkplaceType.Restaurant ? request.RestaurantId : null,
            WarehouseId = request.WarehouseId is > 0 ? request.WarehouseId : null,
            EmailConfirmed = true,
            Code = code,
            RfidCardId = string.IsNullOrWhiteSpace(request.RfidCardId) ? null : request.RfidCardId.Trim(),
            CanAccessAdminPanel = request.CanAccessAdminPanel,
            CanAccessFrontOffice = request.CanAccessFrontOffice
        };

        var res = await _userManager.CreateAsync(user, request.Password);
        if (!res.Succeeded)
        {
            var fe = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            var msg = string.Join(" ", res.Errors.Select(x => x.Description));
            return (false, null, string.IsNullOrEmpty(msg) ? "Could not create the user account." : msg, fe);
        }

        if (employee is not null)
        {
            employee.UserId = user.Id;
            _employeeRepository.Update(employee);
            await _employeeRepository.SaveChangesAsync(cancellationToken);
        }

        var defaultRole = await _roleManager.Roles.FirstOrDefaultAsync(
            r => r.CompanyId == user.CompanyId && r.NormalizedName == "USER", cancellationToken);
        if (defaultRole is not null)
            await AddUserToRoleAsync(user, defaultRole, cancellationToken);

        if (request.RoleIds is { Count: > 0 })
        {
            var roles = await _roleManager.Roles
                .Where(r => request.RoleIds.Contains(r.Id) && r.CompanyId == user.CompanyId)
                .ToListAsync(cancellationToken);
            foreach (var role in roles)
                await AddUserToRoleAsync(user, role, cancellationToken);
        }

        return (true, user.Id, null, null);
    }

    public async Task<(bool Ok, string? Error, Dictionary<string, string[]>? FieldErrors)> UpdateUserAsync(
        int id,
        UpdateUserAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _companyRepository.ExistsAsync(request.CompanyId, cancellationToken))
            return (false, "Company was not found.", null);

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return (false, "User was not found.", null);

        var email = request.Email.Trim();
        var userName = request.UserName.Trim();
        var fullName = request.FullName.Trim();
        if (string.IsNullOrEmpty(fullName))
            return (false, "Full name is required.",
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { ["FullName"] = new[] { "Full name is required." } });

        var byEmail = await _userManager.FindByEmailAsync(email);
        if (byEmail is not null && byEmail.Id != id)
            return (false, "This email is already in use by another user.",
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { ["Email"] = new[] { "This email is already in use." } });
        var byName = await _userManager.FindByNameAsync(userName);
        if (byName is not null && byName.Id != id)
            return (false, "This username is already in use by another user.",
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { ["UserName"] = new[] { "This username is already in use." } });

        var code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();
        if (code is not null)
        {
            if (code.Length != 4 || !code.All(char.IsDigit))
                return (false, "Code must be exactly 4 digits.",
                    new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { ["Code"] = new[] { "Code must be exactly 4 digits." } });

            var codeTaken = await _db.Users.AsNoTracking()
                .AnyAsync(x => x.CompanyId == request.CompanyId && x.Code == code && x.Id != id, cancellationToken);
            if (codeTaken)
                return (false, "This code is already in use in this company.",
                    new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { ["Code"] = new[] { "This code is already in use." } });
        }

        if (request.WorkplaceType == EmployeeWorkplaceType.Restaurant)
        {
            if (!request.RestaurantId.HasValue)
                return (false, "Restaurant must be selected for a restaurant-scoped user.",
                    new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { ["RestaurantId"] = new[] { "Restaurant is required." } });

            var restaurantExists = await _db.Restaurants.AsNoTracking()
                .AnyAsync(r => r.Id == request.RestaurantId.Value && r.CompanyId == request.CompanyId, cancellationToken);
            if (!restaurantExists)
                return (false, "Restaurant was not found for this company.", null);
        }

        if (request.WarehouseId is > 0)
        {
            var warehouseExists = await _db.Warehouses.AsNoTracking()
                .AnyAsync(w => w.Id == request.WarehouseId.Value && w.CompanyId == request.CompanyId, cancellationToken);
            if (!warehouseExists)
                return (false, "Warehouse was not found for this company.", null);
        }

        user.FullName = fullName;
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        user.IsActive = request.IsActive;
        user.CompanyId = request.CompanyId;
        user.Code = code;
        user.RfidCardId = string.IsNullOrWhiteSpace(request.RfidCardId) ? null : request.RfidCardId.Trim();
        user.CanAccessAdminPanel = request.CanAccessAdminPanel;
        user.CanAccessFrontOffice = request.CanAccessFrontOffice;
        user.WorkplaceType = request.WorkplaceType;
        user.RestaurantId = request.WorkplaceType == EmployeeWorkplaceType.Restaurant ? request.RestaurantId : null;
        user.WarehouseId = request.WarehouseId is > 0 ? request.WarehouseId : null;

        var unRes = await _userManager.SetUserNameAsync(user, userName);
        if (!unRes.Succeeded)
            return (false, string.Join(" ", unRes.Errors.Select(x => x.Description)), null);
        var emRes = await _userManager.SetEmailAsync(user, email);
        if (!emRes.Succeeded)
            return (false, string.Join(" ", emRes.Errors.Select(x => x.Description)), null);

        var updateRes = await _userManager.UpdateAsync(user);
        if (!updateRes.Succeeded)
        {
            var msg = string.Join(" ", updateRes.Errors.Select(x => x.Description));
            return (false, string.IsNullOrEmpty(msg) ? "Could not update the user." : msg, null);
        }

        if (!string.IsNullOrEmpty(request.Password))
        {
            var hasPwd = await _userManager.HasPasswordAsync(user);
            if (hasPwd)
            {
                var remove = await _userManager.RemovePasswordAsync(user);
                if (!remove.Succeeded)
                {
                    var m = string.Join(" ", remove.Errors.Select(x => x.Description));
                    return (false, m, null);
                }
            }
            var addPwd = await _userManager.AddPasswordAsync(user, request.Password);
            if (!addPwd.Succeeded)
            {
                var m = string.Join(" ", addPwd.Errors.Select(x => x.Description));
                return (false, m, null);
            }
        }

        var currentEmployee = await _db.Employees.FirstOrDefaultAsync(e => e.UserId == id, cancellationToken);
        if (currentEmployee is not null && (request.EmployeeId is null or 0))
        {
            currentEmployee.UserId = null;
            _employeeRepository.Update(currentEmployee);
        }

        if (request.EmployeeId is > 0)
        {
            if (currentEmployee is not null && currentEmployee.Id != request.EmployeeId)
            {
                currentEmployee.UserId = null;
                _employeeRepository.Update(currentEmployee);
            }

            var newEmp = await _employeeRepository.GetByIdAsync(
                request.EmployeeId.Value,
                request.CompanyId,
                cancellationToken);
            if (newEmp is null)
                return (false, "Employee not found for the selected company.", null);
            if (newEmp.UserId.HasValue && newEmp.UserId != id)
                return (false, "That employee is already linked to a different user.", null);
            newEmp.UserId = id;
            _employeeRepository.Update(newEmp);
        }

        await _employeeRepository.SaveChangesAsync(cancellationToken);

        if (request.RoleIds is not null)
        {
            // Diffed by RoleId (not name) — role names are only unique within a company, so a
            // name-based diff could pick up or drop another tenant's same-named role by mistake.
            var currentRoleIds = await _db.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Select(ur => ur.RoleId)
                .ToListAsync(cancellationToken);
            var targetRoles = await _roleManager.Roles
                .Where(r => request.RoleIds.Contains(r.Id) && r.CompanyId == user.CompanyId)
                .ToListAsync(cancellationToken);
            var targetRoleIds = targetRoles.Select(r => r.Id).ToList();

            var toRemoveIds = currentRoleIds.Except(targetRoleIds).ToList();
            var toAddIds = targetRoleIds.Except(currentRoleIds).ToList();

            if (toRemoveIds.Count > 0)
            {
                var toRemove = await _db.UserRoles
                    .Where(ur => ur.UserId == user.Id && toRemoveIds.Contains(ur.RoleId))
                    .ToListAsync(cancellationToken);
                _db.UserRoles.RemoveRange(toRemove);
            }
            foreach (var roleId in toAddIds)
                _db.UserRoles.Add(new IdentityUserRole<int> { UserId = user.Id, RoleId = roleId });

            if (toRemoveIds.Count > 0 || toAddIds.Count > 0)
                await _db.SaveChangesAsync(cancellationToken);
        }

        return (true, null, null);
    }

    public async Task<(bool Ok, string? Error)> DeleteUserAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return (false, "User was not found.");
        var emps = await _db.Employees.Where(e => e.UserId == id).ToListAsync(cancellationToken);
        foreach (var e in emps)
        {
            e.UserId = null;
            _employeeRepository.Update(e);
        }
        await _employeeRepository.SaveChangesAsync(cancellationToken);
        var res = await _userManager.DeleteAsync(user);
        if (!res.Succeeded)
            return (false, string.Join(" ", res.Errors.Select(x => x.Description)));
        return (true, null);
    }

    public async Task<List<RoleListItemDto>> GetRolesAsync(int? companyId = null, CancellationToken cancellationToken = default)
    {
        // Tenant-scoped: a company only ever sees its own roles. The global SuperAdmin role and
        // the CompanyId = null template roles are never listed here — they aren't editable by anyone
        // through this screen (templates are cloned per-company when a company is created).
        // A SuperAdmin managing another tenant's users/roles can target that company explicitly;
        // a tenant Admin is always confined to their own company regardless of what was requested.
        var effectiveCompanyId = _currentUserService.IsSuperAdmin && companyId is > 0
            ? companyId.Value
            : _currentUserService.CompanyId;

        return await _roleManager.Roles.AsNoTracking()
            .Where(r => r.CompanyId == effectiveCompanyId)
            .OrderBy(r => r.Id)
            .Select(r => new RoleListItemDto
            {
                Id = r.Id,
                Name = r.Name ?? "",
                NormalizedName = r.NormalizedName,
                CompanyId = r.CompanyId
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<AppRole?> FindOwnedRoleAsync(int id, CancellationToken cancellationToken)
    {
        // A SuperAdmin manages roles across every company, so a role lookup by id alone is enough —
        // the id itself already pins the row to its own company. A tenant Admin stays confined to
        // roles that belong to their own company.
        if (_currentUserService.IsSuperAdmin)
            return await _roleManager.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        var companyId = _currentUserService.CompanyId;
        return await _roleManager.Roles.FirstOrDefaultAsync(
            r => r.Id == id && r.CompanyId == companyId, cancellationToken);
    }

    public async Task<RoleListItemDto?> GetRoleByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var r = await FindOwnedRoleAsync(id, cancellationToken);
        if (r is null) return null;
        return new RoleListItemDto
        {
            Id = r.Id,
            Name = r.Name ?? "",
            NormalizedName = r.NormalizedName,
            CompanyId = r.CompanyId
        };
    }

    /// <summary>
    /// "SuperAdmin" is a reserved name — IsSuperAdmin is a plain role-NAME claim check (JWT claims
    /// can't carry CompanyId), so a tenant creating or renaming a role to this exact name would
    /// falsely grant themselves the platform-wide bypass the moment they're assigned to it.
    /// </summary>
    private static bool IsReservedRoleName(string normalizedName) =>
        normalizedName == AppRoles.SuperAdmin.ToUpperInvariant();

    public async Task<(bool Ok, int? RoleId, string? Error)> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrEmpty(name)) return (false, null, "Role name is required.");

        // SuperAdmin may target any company via the form's Company field; a tenant Admin is
        // always confined to their own company regardless of what was submitted.
        var companyId = _currentUserService.IsSuperAdmin && request.CompanyId is > 0
            ? request.CompanyId.Value
            : _currentUserService.CompanyId;
        var normalized = name.ToUpperInvariant();
        if (IsReservedRoleName(normalized))
            return (false, null, "This role name is reserved.");

        var exists = await _roleManager.Roles.AnyAsync(
            r => r.CompanyId == companyId && r.NormalizedName == normalized, cancellationToken);
        if (exists) return (false, null, "A role with this name already exists.");

        var r = new AppRole { Name = name, NormalizedName = normalized, CompanyId = companyId };
        await CreateRoleDirectAsync(r, cancellationToken);
        return (true, r.Id, null);
    }

    public async Task<(bool Ok, string? Error)> UpdateRoleAsync(int id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var r = await FindOwnedRoleAsync(id, cancellationToken);
        if (r is null) return (false, "Role was not found.");
        var newName = request.Name.Trim();
        var newNormalized = newName.ToUpperInvariant();
        if (IsReservedRoleName(newNormalized))
            return (false, "This role name is reserved.");
        var companyId = _currentUserService.CompanyId;
        var nameTaken = await _roleManager.Roles.AnyAsync(
            x => x.Id != id && x.CompanyId == companyId && x.NormalizedName == newNormalized, cancellationToken);
        if (nameTaken) return (false, "A role with this name already exists.");
        r.Name = newName;
        r.NormalizedName = newNormalized;
        var res = await _roleManager.UpdateAsync(r);
        if (!res.Succeeded) return (false, string.Join(" ", res.Errors.Select(x => x.Description)));
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> DeleteRoleAsync(int id, CancellationToken cancellationToken = default)
    {
        var r = await FindOwnedRoleAsync(id, cancellationToken);
        if (r is null) return (false, "Role was not found.");
        if (string.Equals(r.Name, AppRoles.Admin, StringComparison.OrdinalIgnoreCase))
            return (false, "The system administrator role cannot be deleted.");
        var res = await _roleManager.DeleteAsync(r);
        if (!res.Succeeded) return (false, string.Join(" ", res.Errors.Select(x => x.Description)));
        return (true, null);
    }

    public async Task<List<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var query = _db.Permissions.AsNoTracking();
        if (!IsSuperAdmin)
            query = query.Where(x => !PlatformOnlyPermissions.Contains(x.Name));

        return await query
            .OrderBy(x => x.Module)
            .ThenBy(x => x.Action)
            .Select(x => new PermissionDto
            {
                Id = x.Id,
                Name = x.Name,
                DisplayName = x.DisplayName,
                Module = x.Module,
                Action = x.Action
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<int>> GetRolePermissionIdsAsync(int roleId, CancellationToken cancellationToken = default)
    {
        var role = await FindOwnedRoleAsync(roleId, cancellationToken);
        if (role is null) return new List<int>();

        return await _db.RolePermissions
            .AsNoTracking()
            .Where(x => x.RoleId == roleId)
            .Select(x => x.PermissionId)
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Ok, string? Error)> UpdateRolePermissionsAsync(
        int roleId,
        List<int> permissionIds,
        CancellationToken cancellationToken = default)
    {
        var role = await FindOwnedRoleAsync(roleId, cancellationToken);
        if (role is null) return (false, "Role was not found.");

        var distinctIds = permissionIds.Distinct().ToList();

        // Platform-only permissions (managing other companies) can never be granted to a tenant's
        // own roles, even if someone tries to force them through this endpoint directly.
        if (!IsSuperAdmin)
        {
            var blocked = await _db.Permissions
                .Where(x => distinctIds.Contains(x.Id) && PlatformOnlyPermissions.Contains(x.Name))
                .AnyAsync(cancellationToken);
            if (blocked)
                return (false, "These permissions can only be managed by the platform administrator.");
        }

        var validIds = await _db.Permissions
            .AsNoTracking()
            .Where(x => distinctIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (validIds.Count != distinctIds.Count)
            return (false, "Some selected permissions are invalid.");

        var existing = await _db.RolePermissions
            .Where(x => x.RoleId == roleId)
            .ToListAsync(cancellationToken);

        _db.RolePermissions.RemoveRange(existing);
        foreach (var permissionId in distinctIds)
        {
            _db.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        await SyncRolePermissionClaimsAsync(role, cancellationToken);
        return (true, null);
    }

    private async Task SyncRolePermissionClaimsAsync(AppRole role, CancellationToken cancellationToken)
    {
        // Written directly to AspNetRoleClaims — RoleManager.Add/RemoveClaimAsync re-validates the
        // role's name on every single call, which now legitimately fails (multiple companies share
        // role names) and silently drops the claim instead of persisting it.
        var existingClaims = await _db.RoleClaims
            .Where(x => x.RoleId == role.Id && x.ClaimType == "Permission")
            .ToListAsync(cancellationToken);
        var claimPermissions = existingClaims
            .Select(x => x.ClaimValue!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var dbPermissions = await _db.RolePermissions
            .AsNoTracking()
            .Where(x => x.RoleId == role.Id)
            .Select(x => x.Permission.Name)
            .ToListAsync(cancellationToken);

        var stale = existingClaims
            .Where(x => !dbPermissions.Contains(x.ClaimValue, StringComparer.OrdinalIgnoreCase))
            .ToList();
        if (stale.Count > 0)
            _db.RoleClaims.RemoveRange(stale);

        foreach (var missing in dbPermissions.Except(claimPermissions, StringComparer.OrdinalIgnoreCase))
        {
            _db.RoleClaims.Add(new IdentityRoleClaim<int>
            {
                RoleId = role.Id,
                ClaimType = "Permission",
                ClaimValue = missing
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<UserRoleRowDto>> GetUserRoleMappingsAsync(CancellationToken cancellationToken = default)
    {
        var companyId = _currentUserService.CompanyId;
        var query = from ur in _db.UserRoles
                     join u in _db.Users on ur.UserId equals u.Id
                     join role in _db.Roles on ur.RoleId equals role.Id
                     select new { u, role };

        if (!IsSuperAdmin)
            query = query.Where(x => x.u.CompanyId == companyId);

        return await query
            .OrderBy(x => x.u.Id).ThenBy(x => x.role.Name)
            .Select(x => new UserRoleRowDto
            {
                UserId = x.u.Id,
                UserFullName = x.u.FullName,
                UserName = x.u.UserName,
                Email = x.u.Email,
                RoleId = x.role.Id,
                RoleName = x.role.Name ?? ""
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Ok, string? Error)> AssignUserRoleAsync(AssignUserRoleRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null) return (false, "User was not found.");
        var role = await FindOwnedRoleAsync(request.RoleId, cancellationToken);
        if (role is null) return (false, "Role was not found.");
        if (user.CompanyId != _currentUserService.CompanyId) return (false, "User was not found.");
        var alreadyAssigned = await _db.UserRoles.AnyAsync(
            ur => ur.UserId == user.Id && ur.RoleId == role.Id, cancellationToken);
        if (alreadyAssigned) return (false, "The user already has this role.");
        await AddUserToRoleAsync(user, role, cancellationToken);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> RemoveUserRoleAsync(RemoveUserRoleRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null) return (false, "User was not found.");
        var role = await FindOwnedRoleAsync(request.RoleId, cancellationToken);
        if (role is null) return (false, "Role was not found.");
        if (user.CompanyId != _currentUserService.CompanyId) return (false, "User was not found.");
        await RemoveUserFromRoleAsync(user, role, cancellationToken);
        return (true, null);
    }

    public async Task CloneDefaultRolesForCompanyAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var alreadyProvisioned = await _roleManager.Roles.AnyAsync(r => r.CompanyId == companyId, cancellationToken);
        if (alreadyProvisioned)
            return;

        // Only the Admin template is auto-cloned for a new company — the tenant's own Admin
        // creates whatever additional roles (Waiter, Kitchen, ...) their business actually needs
        // from the Roles screen, instead of the platform pre-creating a fixed set for everyone.
        var templates = await _roleManager.Roles
            .Where(r => r.CompanyId == null && r.NormalizedName == AppRoles.Admin.ToUpperInvariant())
            .ToListAsync(cancellationToken);

        foreach (var template in templates)
        {
            var clone = new AppRole { Name = template.Name, NormalizedName = template.NormalizedName, CompanyId = companyId };
            await CreateRoleDirectAsync(clone, cancellationToken);

            var templatePermissionIds = await _db.RolePermissions
                .AsNoTracking()
                .Where(x => x.RoleId == template.Id)
                .Select(x => x.PermissionId)
                .ToListAsync(cancellationToken);

            foreach (var permissionId in templatePermissionIds)
            {
                _db.RolePermissions.Add(new RolePermission { RoleId = clone.Id, PermissionId = permissionId });
            }
            await _db.SaveChangesAsync(cancellationToken);

            await SyncRolePermissionClaimsAsync(clone, cancellationToken);
        }
    }
}