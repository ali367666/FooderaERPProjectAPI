using Application.Common.Interfaces;
using Application.Common.Responce;
using Application.IdentityAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.Constants;

namespace API.Controllers;

[Route("api/permissions")]
[ApiController]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IIdentityAdminService _identityAdminService;

    public PermissionsController(IIdentityAdminService identityAdminService)
    {
        _identityAdminService = identityAdminService;
    }

    [Authorize(Policy = AppPermissions.UserView)]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var list = await _identityAdminService.GetPermissionsAsync(cancellationToken);
        return Ok(BaseResponse<List<PermissionDto>>.Ok(list, "Permissions loaded."));
    }
}
