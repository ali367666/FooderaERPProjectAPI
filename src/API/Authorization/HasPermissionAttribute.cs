using Microsoft.AspNetCore.Authorization;

namespace API.Authorization;

public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
    {
        Policy = $"{PermissionPolicyProvider.PolicyPrefix}{permission}";
    }
}
