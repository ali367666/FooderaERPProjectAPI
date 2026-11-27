using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace API.Authorization;

/// <summary>
/// A signed-in user whose role lacks the permission an endpoint needs gets a clear answer instead of an empty 403:
/// the screen can say "your role has no right to this section" and name the permission.
/// </summary>
public sealed class PermissionResultHandler : IAuthorizationMiddlewareResultHandler
{
    public const string Message = "Rolunuzun bu bölməyə hüququ yoxdur.";

    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden && context.User.Identity?.IsAuthenticated == true)
        {
            var permission = authorizeResult.AuthorizationFailure?.FailedRequirements
                .OfType<PermissionRequirement>().FirstOrDefault()?.Permission;

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { success = false, message = Message, permission });
            return;
        }

        await _default.HandleAsync(next, context, policy, authorizeResult);
    }
}
