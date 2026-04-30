using Application.Common.Interfaces;
using Application.Common.Responce;
using Application.IdentityAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.Constants;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UserRolesController : ControllerBase
{
    private readonly IIdentityAdminService _identityAdminService;

    public UserRolesController(IIdentityAdminService identityAdminService)
    {
        _identityAdminService = identityAdminService;
    }

    [Authorize(Policy = AppPermissions.UserView)]
    [HttpGet]
    public async Task<IActionResult> GetMappings(CancellationToken cancellationToken)
    {
        var list = await _identityAdminService.GetUserRoleMappingsAsync(cancellationToken);
        return Ok(BaseResponse<List<UserRoleRowDto>>.Ok(list, "User roles loaded."));
    }

    [Authorize(Policy = AppPermissions.UserUpdate)]
    [HttpPost("assign")]
    public async Task<IActionResult> Assign([FromBody] AssignUserRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await _identityAdminService.AssignUserRoleAsync(request, cancellationToken);
        if (!result.Ok)
            return BadRequest(BaseResponse.Fail(result.Error ?? "Could not assign role."));
        return Ok(BaseResponse.Ok("Role assigned successfully."));
    }

    [Authorize(Policy = AppPermissions.UserUpdate)]
    [HttpPost("remove")]
    public async Task<IActionResult> Remove([FromBody] RemoveUserRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await _identityAdminService.RemoveUserRoleAsync(request, cancellationToken);
        if (!result.Ok)
            return BadRequest(BaseResponse.Fail(result.Error ?? "Could not remove role."));
        return Ok(BaseResponse.Ok("Role removed successfully."));
    }
}
