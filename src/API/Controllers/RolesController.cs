using Application.Common.Interfaces;
using Application.Common.Responce;
using Application.IdentityAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.Constants;

namespace API.Controllers;

[Route("api/roles")]
[ApiController]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IIdentityAdminService _identityAdminService;

    public RolesController(IIdentityAdminService identityAdminService)
    {
        _identityAdminService = identityAdminService;
    }

    [Authorize(Policy = AppPermissions.RoleView)]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var list = await _identityAdminService.GetRolesAsync(cancellationToken);
        return Ok(BaseResponse<List<RoleListItemDto>>.Ok(list, "Roles loaded."));
    }

    [Authorize(Policy = AppPermissions.RoleView)]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var role = await _identityAdminService.GetRoleByIdAsync(id, cancellationToken);
        if (role is null)
            return NotFound(BaseResponse<RoleListItemDto>.Fail("Role was not found."));
        return Ok(BaseResponse<RoleListItemDto>.Ok(role));
    }

    [Authorize(Policy = AppPermissions.UserCreate)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await _identityAdminService.CreateRoleAsync(request, cancellationToken);
        if (!result.Ok)
            return BadRequest(BaseResponse.Fail(result.Error ?? "Could not create role."));
        return Ok(BaseResponse<int>.Ok(result.RoleId!.Value, "Role created successfully."));
    }

    [Authorize(Policy = AppPermissions.UserUpdate)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await _identityAdminService.UpdateRoleAsync(id, request, cancellationToken);
        if (!result.Ok)
            return BadRequest(BaseResponse.Fail(result.Error ?? "Could not update role."));
        return Ok(BaseResponse.Ok("Role updated successfully."));
    }

    [Authorize(Policy = AppPermissions.UserDelete)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await _identityAdminService.DeleteRoleAsync(id, cancellationToken);
        if (!result.Ok)
            return BadRequest(BaseResponse.Fail(result.Error ?? "Could not delete role."));
        return Ok(BaseResponse.Ok("Role deleted successfully."));
    }

    [Authorize(Policy = AppPermissions.RoleView)]
    [HttpGet("{roleId:int}/permissions")]
    public async Task<IActionResult> GetRolePermissions([FromRoute] int roleId, CancellationToken cancellationToken)
    {
        var ids = await _identityAdminService.GetRolePermissionIdsAsync(roleId, cancellationToken);
        return Ok(BaseResponse<List<int>>.Ok(ids, "Role permissions loaded."));
    }

    [Authorize(Policy = AppPermissions.UserRoleManage)]
    [HttpPut("{roleId:int}/permissions")]
    public async Task<IActionResult> UpdateRolePermissions(
        [FromRoute] int roleId,
        [FromBody] UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _identityAdminService.UpdateRolePermissionsAsync(roleId, request.PermissionIds, cancellationToken);
        if (!result.Ok)
            return BadRequest(BaseResponse.Fail(result.Error ?? "Could not update role permissions."));
        return Ok(BaseResponse.Ok("Role permissions updated successfully."));
    }
}
