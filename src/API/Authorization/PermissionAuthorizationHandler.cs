using Microsoft.AspNetCore.Authorization;

namespace API.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.HasClaim("Permission", requirement.Permission))
            context.Succeed(requirement);
        // The platform SuperAdmin opens any company's store screens without a role of that company holding the Store permissions.
        else if (requirement.Permission.StartsWith("Store", StringComparison.Ordinal) && context.User.IsInRole(Domain.Constants.AppRoles.SuperAdmin))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
