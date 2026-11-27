using Application.Common.Interfaces;
using Application.MyProfile;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity;

public class ProfileService : IProfileService
{
    private readonly AppDbContext _db;

    public ProfileService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ProfileDto?> GetAsync(int userId, CancellationToken cancellationToken)
    {
        var profile = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new ProfileDto
            {
                Id = u.Id,
                FullName = u.FullName,
                UserName = u.UserName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Code = u.Code,
                AvatarUrl = u.AvatarUrl,
                WorkplaceType = u.WorkplaceType,
                RestaurantName = u.Restaurant != null ? u.Restaurant.Name : null,
                WarehouseName = u.Warehouse != null ? u.Warehouse.Name : null,
                CompanyId = u.CompanyId,
                CompanyName = u.Company.Name,
                CompanyCode = u.Company.CompanyCode,
                CompanyTaxNumber = u.Company.TaxNumber,
                CompanyAddress = u.Company.Address,
                CompanyPhoneNumber = u.Company.PrimaryPhoneNumber,
                CompanyEmail = u.Company.Email
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
            return null;

        profile.Roles = await (
            from ur in _db.UserRoles
            join r in _db.Roles on ur.RoleId equals r.Id
            where ur.UserId == userId
            orderby r.Name
            select r.Name!).ToListAsync(cancellationToken);

        var employee = await _db.Employees.AsNoTracking()
            .Where(e => e.UserId == userId)
            .Select(e => new { Department = e.Department.Name, Position = e.Position.Name, e.HireDate })
            .FirstOrDefaultAsync(cancellationToken);

        if (employee is not null)
        {
            profile.DepartmentName = employee.Department;
            profile.PositionName = employee.Position;
            profile.HireDate = employee.HireDate;
        }

        return profile;
    }

    public async Task<bool> SetAvatarAsync(int userId, string? avatarUrl, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return false;

        user.AvatarUrl = avatarUrl;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
