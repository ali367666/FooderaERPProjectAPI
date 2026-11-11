using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity;

/// <summary>
/// A company role is the employee's job title: each role has one <see cref="Position"/> of the same name that follows it
/// (created with the role, renamed with it, released when the role goes). Existing positions are folded in by name.
/// </summary>
public static class RolePositionSync
{
    public const string DefaultDepartmentName = "Ümumi";

    private static readonly HashSet<string> ReservedRoleNames = new(StringComparer.OrdinalIgnoreCase)
    {
        Domain.Constants.AppRoles.SuperAdmin,
        Domain.Constants.AppRoles.CompanySuperAdmin,
    };

    /// <summary>The position that stands for the role: the linked one, else an unlinked one of the same name, else a new one.</summary>
    public static async Task<Position> EnsureForRoleAsync(AppDbContext db, int companyId, AppRole role, CancellationToken ct)
    {
        var linked = await db.Positions.FirstOrDefaultAsync(p => p.CompanyId == companyId && p.RoleId == role.Id, ct);
        if (linked is not null)
        {
            await RenameAsync(db, linked, role.Name!, ct);
            return linked;
        }

        var byName = await db.Positions
            .Where(p => p.CompanyId == companyId && p.RoleId == null && p.Name == role.Name)
            .OrderBy(p => p.Id)
            .FirstOrDefaultAsync(ct);
        if (byName is not null)
        {
            byName.RoleId = role.Id;
            await db.SaveChangesAsync(ct);
            return byName;
        }

        var department = await db.Departments.FirstOrDefaultAsync(d => d.CompanyId == companyId && d.Name == DefaultDepartmentName, ct);
        if (department is null)
        {
            department = new Department { CompanyId = companyId, Name = DefaultDepartmentName };
            db.Departments.Add(department);
            await db.SaveChangesAsync(ct);
        }

        var position = new Position { CompanyId = companyId, DepartmentId = department.Id, Name = role.Name!, RoleId = role.Id };
        db.Positions.Add(position);
        await db.SaveChangesAsync(ct);
        return position;
    }

    private static async Task RenameAsync(AppDbContext db, Position position, string name, CancellationToken ct)
    {
        if (position.Name == name) return;
        // (company, department, name) is unique: skip the rename when another position of that department already has the name.
        var taken = await db.Positions.AnyAsync(p => p.CompanyId == position.CompanyId && p.DepartmentId == position.DepartmentId && p.Name == name && p.Id != position.Id, ct);
        if (taken) return;
        position.Name = name;
        await db.SaveChangesAsync(ct);
    }

    public static async Task RenameForRoleAsync(AppDbContext db, int roleId, string name, CancellationToken ct)
    {
        var position = await db.Positions.FirstOrDefaultAsync(p => p.RoleId == roleId, ct);
        if (position is not null)
            await RenameAsync(db, position, name, ct);
    }

    /// <summary>The role is going away: its position goes too when nobody holds it, otherwise it stays as an ordinary position.</summary>
    public static async Task ReleaseForRoleAsync(AppDbContext db, int roleId, CancellationToken ct)
    {
        var positions = await db.Positions.Where(p => p.RoleId == roleId).ToListAsync(ct);
        foreach (var position in positions)
        {
            if (await db.Employees.AnyAsync(e => e.PositionId == position.Id, ct))
                position.RoleId = null;
            else
                db.Positions.Remove(position);
        }
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// One-off and idempotent: every company role gets its position, and every old position without a role becomes a role
    /// (empty permission set) of the same name, so nothing is left outside the one list.
    /// </summary>
    public static async Task BackfillAsync(AppDbContext db, int companyId, CancellationToken ct)
    {
        var roles = await db.Roles.Where(r => r.CompanyId == companyId).ToListAsync(ct);
        foreach (var role in roles)
            await EnsureForRoleAsync(db, companyId, role, ct);

        var loose = await db.Positions.Where(p => p.CompanyId == companyId && p.RoleId == null).ToListAsync(ct);
        foreach (var position in loose)
        {
            var normalized = position.Name.ToUpperInvariant();
            if (ReservedRoleNames.Contains(position.Name)) continue;

            var role = await db.Roles.FirstOrDefaultAsync(r => r.CompanyId == companyId && r.NormalizedName == normalized, ct);
            if (role is null)
            {
                role = new AppRole { Name = position.Name, NormalizedName = normalized, CompanyId = companyId };
                db.Roles.Add(role);
                await db.SaveChangesAsync(ct);
            }

            // Another position may already stand for that role (same name in two departments): keep one link per role.
            if (!await db.Positions.AnyAsync(p => p.CompanyId == companyId && p.RoleId == role.Id, ct))
            {
                position.RoleId = role.Id;
                await db.SaveChangesAsync(ct);
            }
        }
    }
}
