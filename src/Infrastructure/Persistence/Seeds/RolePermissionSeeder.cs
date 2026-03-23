using Domain.Constants;

namespace Infrastructure.Identity;

public static class RolePermissionSeeder
{
    public static readonly Dictionary<string, List<string>> Permissions = new()
    {
        {
            AppRoles.Admin,
            new List<string>
            {
                AppPermissions.CompanyView,
                AppPermissions.CompanyCreate,
                AppPermissions.CompanyUpdate,
                AppPermissions.CompanyDelete,

                AppPermissions.UserView,
                AppPermissions.UserCreate,
                AppPermissions.UserUpdate,
                AppPermissions.UserDelete,

                AppPermissions.RestaurantView,
                AppPermissions.RestaurantCreate,
                AppPermissions.RestaurantUpdate,
                AppPermissions.RestaurantDelete
            }
        },
        {
            AppRoles.User,
            new List<string>
            {
                AppPermissions.CompanyView,
                AppPermissions.UserView,
                AppPermissions.RestaurantView
            }
        }
    };
}