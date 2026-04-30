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
public class UsersController : ControllerBase
{
    private readonly IIdentityAdminService _identityAdminService;

    public UsersController(IIdentityAdminService identityAdminService)
    {
        _identityAdminService = identityAdminService;
    }

    [Authorize(Policy = AppPermissions.UserView)]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? companyId, CancellationToken cancellationToken)
    {
        var list = await _identityAdminService.GetUsersAsync(companyId, cancellationToken);
        return Ok(BaseResponse<List<UserListItemDto>>.Ok(list, "Users loaded."));
    }

    [Authorize(Policy = AppPermissions.UserView)]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var user = await _identityAdminService.GetUserByIdAsync(id, cancellationToken);
        if (user is null)
            return NotFound(BaseResponse<UserDetailDto>.Fail("User was not found."));
        return Ok(BaseResponse<UserDetailDto>.Ok(user));
    }

    [Authorize(Policy = AppPermissions.UserCreate)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserAdminRequest request, CancellationToken cancellationToken)
    {
        var result = await _identityAdminService.CreateUserAsync(request, cancellationToken);
        if (!result.Ok)
        {
            if (result.FieldErrors is { Count: > 0 })
                return BadRequest(new { success = false, message = result.Error, errors = result.FieldErrors });
            return BadRequest(BaseResponse.Fail(result.Error ?? "Could not create user."));
        }

        return Ok(BaseResponse<int>.Ok(result.UserId!.Value, "User created successfully."));
    }

    [Authorize(Policy = AppPermissions.UserUpdate)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateUserAdminRequest request, CancellationToken cancellationToken)
    {
        var result = await _identityAdminService.UpdateUserAsync(id, request, cancellationToken);
        if (!result.Ok)
        {
            if (result.FieldErrors is { Count: > 0 })
                return BadRequest(new { success = false, message = result.Error, errors = result.FieldErrors });
            return BadRequest(BaseResponse.Fail(result.Error ?? "Could not update user."));
        }

        return Ok(BaseResponse.Ok("User updated successfully."));
    }

    [Authorize(Policy = AppPermissions.UserDelete)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await _identityAdminService.DeleteUserAsync(id, cancellationToken);
        if (!result.Ok)
            return BadRequest(BaseResponse.Fail(result.Error ?? "Could not delete user."));
        return Ok(BaseResponse.Ok("User deleted successfully."));
    }
}
