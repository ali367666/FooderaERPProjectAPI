using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.IdentityAdmin;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Infrastructure.Identity;

public class IdentityAdminService : IIdentityAdminService
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<int>> _roleManager;
    private readonly AppDbContext _db;
    private readonly ICompanyRepository _companyRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public IdentityAdminService(
        UserManager<User> userManager,
        RoleManager<IdentityRole<int>> roleManager,
        AppDbContext db,
        ICompanyRepository companyRepository,
        IEmployeeRepository employeeRepository)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
        _companyRepository = companyRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<List<UserListItemDto>> GetUsersAsync(int? companyIdFilter, CancellationToken cancellationToken = default)
    {
        var q = _db.Users.AsNoTracking().Include(u => u.Company).AsQueryable();
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
                LinkedEmployeeId = emp?.Id
            });
        }

        return result;
    }

    public async Task<UserDetailDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var u = await _db.Users.AsNoTracking()
            .Include(x => x.Company)
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
            LinkedEmployeeId = emp?.Id
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

        var user = new User
        {
            UserName = userName,
            Email = email,
            FullName = fullName,
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            IsActive = request.IsActive,
            CompanyId = request.CompanyId,
            WorkplaceType = EmployeeWorkplaceType.HeadOffice,
            RestaurantId = null,
            EmailConfirmed = true
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

        var u = user;
        var roleName = "User";
        if (await _roleManager.RoleExistsAsync(roleName))
        {
            await _userManager.AddToRoleAsync(user, roleName);
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

        user.FullName = fullName;
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        user.IsActive = request.IsActive;
        user.CompanyId = request.CompanyId;

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

    public async Task<List<RoleListItemDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _roleManager.Roles.AsNoTracking()
            .OrderBy(r => r.Id)
            .Select(r => new RoleListItemDto
            {
                Id = r.Id,
                Name = r.Name ?? "",
                NormalizedName = r.NormalizedName
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<RoleListItemDto?> GetRoleByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var r = await _roleManager.FindByIdAsync(id.ToString());
        if (r is null) return null;
        return new RoleListItemDto
        {
            Id = r.Id,
            Name = r.Name ?? "",
            NormalizedName = r.NormalizedName
        };
    }

    public async Task<(bool Ok, int? RoleId, string? Error)> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrEmpty(name)) return (false, null, "Role name is required.");
        if (await _roleManager.RoleExistsAsync(name)) return (false, null, "A role with this name already exists.");
        var r = new IdentityRole<int> { Name = name, NormalizedName = name.ToUpperInvariant() };
        var res = await _roleManager.CreateAsync(r);
        if (!res.Succeeded) return (false, null, string.Join(" ", res.Errors.Select(x => x.Description)));
        return (true, r.Id, null);
    }

    public async Task<(bool Ok, string? Error)> UpdateRoleAsync(int id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var r = await _roleManager.FindByIdAsync(id.ToString());
        if (r is null) return (false, "Role was not found.");
        var newName = request.Name.Trim();
        r.Name = newName;
        r.NormalizedName = newName.ToUpperInvariant();
        var res = await _roleManager.UpdateAsync(r);
        if (!res.Succeeded) return (false, string.Join(" ", res.Errors.Select(x => x.Description)));
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> DeleteRoleAsync(int id, CancellationToken cancellationToken = default)
    {
        var r = await _roleManager.FindByIdAsync(id.ToString());
        if (r is null) return (false, "Role was not found.");
        if (string.Equals(r.Name, "Admin", StringComparison.OrdinalIgnoreCase))
            return (false, "The system administrator role cannot be deleted.");
        var res = await _roleManager.DeleteAsync(r);
        if (!res.Succeeded) return (false, string.Join(" ", res.Errors.Select(x => x.Description)));
        return (true, null);
    }

    public async Task<List<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Permissions
            .AsNoTracking()
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
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
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
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role is null) return (false, "Role was not found.");

        var distinctIds = permissionIds.Distinct().ToList();
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

    private async Task SyncRolePermissionClaimsAsync(IdentityRole<int> role, CancellationToken cancellationToken)
    {
        var claimPermissions = (await _roleManager.GetClaimsAsync(role))
            .Where(x => x.Type == "Permission")
            .Select(x => x.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var dbPermissions = await _db.RolePermissions
            .AsNoTracking()
            .Where(x => x.RoleId == role.Id)
            .Select(x => x.Permission.Name)
            .ToListAsync(cancellationToken);

        foreach (var stale in claimPermissions.Except(dbPermissions, StringComparer.OrdinalIgnoreCase).ToList())
            await _roleManager.RemoveClaimAsync(role, new Claim("Permission", stale));

        foreach (var missing in dbPermissions.Except(claimPermissions, StringComparer.OrdinalIgnoreCase).ToList())
            await _roleManager.AddClaimAsync(role, new Claim("Permission", missing));
    }

    public async Task<List<UserRoleRowDto>> GetUserRoleMappingsAsync(CancellationToken cancellationToken = default)
    {
        return await (from ur in _db.UserRoles
            join u in _db.Users on ur.UserId equals u.Id
            join role in _db.Roles on ur.RoleId equals role.Id
            orderby u.Id, role.Name
            select new UserRoleRowDto
            {
                UserId = u.Id,
                UserFullName = u.FullName,
                UserName = u.UserName,
                Email = u.Email,
                RoleId = role.Id,
                RoleName = role.Name ?? ""
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Ok, string? Error)> AssignUserRoleAsync(AssignUserRoleRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null) return (false, "User was not found.");
        var role = await _roleManager.FindByIdAsync(request.RoleId.ToString());
        if (role is null) return (false, "Role was not found.");
        if (await _userManager.IsInRoleAsync(user, role.Name!)) return (false, "The user already has this role.");
        var res = await _userManager.AddToRoleAsync(user, role.Name!);
        if (!res.Succeeded) return (false, string.Join(" ", res.Errors.Select(x => x.Description)));
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> RemoveUserRoleAsync(RemoveUserRoleRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null) return (false, "User was not found.");
        var role = await _roleManager.FindByIdAsync(request.RoleId.ToString());
        if (role is null) return (false, "Role was not found.");
        var res = await _userManager.RemoveFromRoleAsync(user, role.Name!);
        if (!res.Succeeded) return (false, string.Join(" ", res.Errors.Select(x => x.Description)));
        return (true, null);
    }
}
