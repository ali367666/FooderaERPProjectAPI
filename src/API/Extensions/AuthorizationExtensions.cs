using Domain.Constants;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace API.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddPermissionPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            var permissionValues = typeof(AppPermissions)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(x => x.IsLiteral && !x.IsInitOnly && x.FieldType == typeof(string))
                .Select(x => (string)x.GetRawConstantValue()!)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var permission in permissionValues)
            {
                options.AddPolicy(permission, policy => policy.RequireClaim("Permission", permission));
            }
        });

        return services;
    }
}