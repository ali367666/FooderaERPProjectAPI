using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<User> _userManager;

    public IdentityService(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email) is not null;
    }

    public async Task<bool> UserNameExistsAsync(string userName)
    {
        return await _userManager.FindByNameAsync(userName) is not null;
    }

    public async Task<(bool Succeeded, int UserId, List<string> Errors)> CreateUserAsync(
        string fullName,
        string userName,
        string email,
        string password,
        EmployeeWorkplaceType workplaceType,
        int companyId,
        int? restaurantId)
    {
        var user = new User
        {
            FullName = fullName,
            UserName = userName,
            Email = email,
            WorkplaceType = workplaceType,
            CompanyId = companyId,
            RestaurantId = restaurantId,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            return (false, 0, result.Errors.Select(x => x.Description).ToList());
        }

        return (true, user.Id, new List<string>());
    }

    public async Task<bool> AddToRoleAsync(int userId, string roleName)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);

        if (user is null)
            return false;

        var result = await _userManager.AddToRoleAsync(user, roleName);

        return result.Succeeded;
    }

    public async Task<Domain.Entities.User?> GetUserByIdAsync(int id)
    {
        return await _userManager.FindByIdAsync(id.ToString());
    }

    public async Task<(bool Succeeded, List<string> Errors)> UpdateUserAsync(
        int id,
        string fullName,
        EmployeeWorkplaceType workplaceType,
        int companyId,
        int? restaurantId)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user is null)
        {
            return (false, new List<string> { "User tapılmadı." });
        }

        user.FullName = fullName;
        user.WorkplaceType = workplaceType;
        user.CompanyId = companyId;
        user.RestaurantId = restaurantId;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return (false, result.Errors.Select(x => x.Description).ToList());
        }

        return (true, new List<string>());
    }

    public async Task<(bool Succeeded, List<string> Errors)> DeleteUserAsync(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user is null)
        {
            return (false, new List<string> { "User tapılmadı." });
        }

        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            return (false, result.Errors.Select(x => x.Description).ToList());
        }

        return (true, new List<string>());
    }
}