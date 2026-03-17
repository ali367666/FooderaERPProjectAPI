using Domain.Enums;

namespace Application.Common.Interfaces;

public interface IIdentityService
{
    Task<bool> EmailExistsAsync(string email);
    Task<bool> UserNameExistsAsync(string userName);
    Task<(bool Succeeded, int UserId, List<string> Errors)> CreateUserAsync(
        string fullName,
        string userName,
        string email,
        string password,
        EmployeeWorkplaceType workplaceType,
        int companyId,
        int? restaurantId);

    Task<bool> AddToRoleAsync(int userId, string roleName);

    Task<Domain.Entities.User?> GetUserByIdAsync(int id);

    Task<(bool Succeeded, List<string> Errors)> UpdateUserAsync(
        int id,
        string fullName,
        EmployeeWorkplaceType workplaceType,
        int companyId,
        int? restaurantId);

    Task<(bool Succeeded, List<string> Errors)> DeleteUserAsync(int id);
}