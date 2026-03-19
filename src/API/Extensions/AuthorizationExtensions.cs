using Domain.Constants;
using Microsoft.Extensions.DependencyInjection;

namespace API.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddPermissionPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("CompanyView",
                policy => policy.RequireClaim("Permission", AppPermissions.CompanyView));

            options.AddPolicy("CompanyCreate",
                policy => policy.RequireClaim("Permission", AppPermissions.CompanyCreate));

            options.AddPolicy("CompanyUpdate",
                policy => policy.RequireClaim("Permission", AppPermissions.CompanyUpdate));

            options.AddPolicy("CompanyDelete",
                policy => policy.RequireClaim("Permission", AppPermissions.CompanyDelete));

            options.AddPolicy("UserView",
                policy => policy.RequireClaim("Permission", AppPermissions.UserView));

            options.AddPolicy("UserCreate",
                policy => policy.RequireClaim("Permission", AppPermissions.UserCreate));

            options.AddPolicy("UserUpdate",
                policy => policy.RequireClaim("Permission", AppPermissions.UserUpdate));

            options.AddPolicy("UserDelete",
                policy => policy.RequireClaim("Permission", AppPermissions.UserDelete));
        });

        return services;
    }
}