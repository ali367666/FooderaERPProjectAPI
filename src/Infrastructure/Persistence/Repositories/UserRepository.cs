using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Users
            .AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<User?> GetByCompanyAndCodeAsync(int companyId, string code, CancellationToken cancellationToken)
    {
        return await _context.Users
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Code == code, cancellationToken);
    }

    public async Task<User?> GetByRfidCardIdAsync(string rfidCardId, CancellationToken cancellationToken)
    {
        return await _context.Users
            .FirstOrDefaultAsync(x => x.RfidCardId == rfidCardId, cancellationToken);
    }

    public async Task<bool> HasRotatingPinRoleAsync(int userId, CancellationToken cancellationToken)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.RequiresRotatingPin)
            .AnyAsync(requiresRotatingPin => requiresRotatingPin, cancellationToken);
    }

    /// <summary>
    /// Mirrors how the login token is built: the permission comes from one of the user's roles
    /// (own company or global) and, outside the platform SuperAdmin, must also exist on the shared
    /// CompanySuperAdmin template — the ceiling no company user can exceed.
    /// </summary>
    public async Task<bool> HasPermissionAsync(int userId, string permission, CancellationToken cancellationToken)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null) return false;

        var roles = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r)
            .Where(r => r.CompanyId == user.CompanyId || r.CompanyId == null)
            .Select(r => new { r.Id, r.NormalizedName })
            .ToListAsync(cancellationToken);
        if (roles.Count == 0) return false;

        var roleIds = roles.Select(r => r.Id).ToList();
        var granted = await _context.RolePermissions
            .AnyAsync(rp => roleIds.Contains(rp.RoleId) && rp.Permission.Name == permission, cancellationToken);
        if (!granted) return false;

        if (roles.Any(r => r.NormalizedName == Domain.Constants.AppRoles.SuperAdmin.ToUpperInvariant()))
            return true;

        var templateNormalized = Domain.Constants.AppRoles.CompanySuperAdmin.ToUpperInvariant();
        return await _context.RolePermissions.AnyAsync(
            rp => rp.Role.CompanyId == null && rp.Role.NormalizedName == templateNormalized && rp.Permission.Name == permission,
            cancellationToken);
    }

    public async Task<List<User>> GetAllByWarehouseIdAsync(int warehouseId, CancellationToken cancellationToken)
    {
        return await _context.Users
            .Where(x => x.WarehouseId == warehouseId)
            .ToListAsync(cancellationToken);
    }

    public void Update(User user) => _context.Users.Update(user);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}