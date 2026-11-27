using Domain.Entities;
using Infrastructure.Identity;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests;

/// <summary>A role is the employee's job title: each company role has a position of the same name that follows it.</summary>
public class RolePositionSyncTests
{
    private const int Company = 1;

    private static AppDbContext NewDb()
        => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<AppRole> AddRole(AppDbContext db, string name, int companyId = Company)
    {
        var role = new AppRole { Name = name, NormalizedName = name.ToUpperInvariant(), CompanyId = companyId };
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        return role;
    }

    [Fact]
    public async Task A_new_role_gets_a_position_in_the_default_department()
    {
        var db = NewDb();
        var role = await AddRole(db, "Ofisiant");

        var position = await RolePositionSync.EnsureForRoleAsync(db, Company, role, default);

        Assert.Equal("Ofisiant", position.Name);
        Assert.Equal(role.Id, position.RoleId);
        Assert.Equal(RolePositionSync.DefaultDepartmentName, (await db.Departments.SingleAsync()).Name);
    }

    [Fact]
    public async Task An_old_position_of_the_same_name_is_taken_over_not_duplicated()
    {
        var db = NewDb();
        var department = new Department { CompanyId = Company, Name = "Zal" };
        db.Departments.Add(department);
        await db.SaveChangesAsync();
        db.Positions.Add(new Position { Id = 5, CompanyId = Company, DepartmentId = department.Id, Name = "Ofisiant" });
        await db.SaveChangesAsync();
        var role = await AddRole(db, "Ofisiant");

        var position = await RolePositionSync.EnsureForRoleAsync(db, Company, role, default);

        Assert.Equal(5, position.Id);
        Assert.Equal(role.Id, position.RoleId);
        Assert.Equal(1, await db.Positions.CountAsync());
    }

    [Fact]
    public async Task Ensuring_twice_is_harmless()
    {
        var db = NewDb();
        var role = await AddRole(db, "Aşpaz");

        await RolePositionSync.EnsureForRoleAsync(db, Company, role, default);
        await RolePositionSync.EnsureForRoleAsync(db, Company, role, default);

        Assert.Equal(1, await db.Positions.CountAsync());
    }

    [Fact]
    public async Task Renaming_the_role_renames_its_position()
    {
        var db = NewDb();
        var role = await AddRole(db, "Kassir");
        await RolePositionSync.EnsureForRoleAsync(db, Company, role, default);

        await RolePositionSync.RenameForRoleAsync(db, role.Id, "Baş kassir", default);

        Assert.Equal("Baş kassir", (await db.Positions.SingleAsync()).Name);
    }

    [Fact]
    public async Task A_deleted_role_takes_an_unused_position_with_it_but_keeps_one_an_employee_holds()
    {
        var db = NewDb();
        var unused = await AddRole(db, "Köhnə");
        var used = await AddRole(db, "İşləyən");
        await RolePositionSync.EnsureForRoleAsync(db, Company, unused, default);
        var usedPosition = await RolePositionSync.EnsureForRoleAsync(db, Company, used, default);
        db.Employees.Add(new Employee { Id = 1, CompanyId = Company, FirstName = "A", LastName = "B", PositionId = usedPosition.Id, DepartmentId = usedPosition.DepartmentId });
        await db.SaveChangesAsync();

        await RolePositionSync.ReleaseForRoleAsync(db, unused.Id, default);
        await RolePositionSync.ReleaseForRoleAsync(db, used.Id, default);

        var left = await db.Positions.SingleAsync();
        Assert.Equal("İşləyən", left.Name);
        Assert.Null(left.RoleId);
    }

    [Fact]
    public async Task Backfill_gives_roles_positions_and_positions_roles()
    {
        var db = NewDb();
        await AddRole(db, "Menecer");
        var department = new Department { CompanyId = Company, Name = "Mətbəx" };
        db.Departments.Add(department);
        await db.SaveChangesAsync();
        db.Positions.Add(new Position { CompanyId = Company, DepartmentId = department.Id, Name = "Aşpaz" });
        await db.SaveChangesAsync();

        await RolePositionSync.BackfillAsync(db, Company, default);
        await RolePositionSync.BackfillAsync(db, Company, default); // idempotent

        Assert.Equal(new[] { "Aşpaz", "Menecer" }, db.Roles.Where(r => r.CompanyId == Company).Select(r => r.Name).OrderBy(n => n).ToArray());
        Assert.All(await db.Positions.ToListAsync(), p => Assert.NotNull(p.RoleId));
        Assert.Equal(2, await db.Positions.CountAsync());
    }

    [Fact]
    public async Task Another_companys_roles_are_left_alone()
    {
        var db = NewDb();
        await AddRole(db, "Menecer", companyId: 2);

        await RolePositionSync.BackfillAsync(db, Company, default);

        Assert.Empty(db.Positions);
    }
}
