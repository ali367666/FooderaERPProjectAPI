using Application.BackOffice;
using Application.Common.Interfaces;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/Employees/leaves")]
public class EmployeeLeavesController : ControllerBase
{
    private readonly IEmployeeLeaveService _leaves;
    private readonly ICurrentUserService _currentUser;

    public EmployeeLeavesController(IEmployeeLeaveService leaves, ICurrentUserService currentUser)
    {
        _leaves = leaves;
        _currentUser = currentUser;
    }

    [Authorize(Policy = AppPermissions.EmployeeView)]
    [HttpGet]
    public async Task<ActionResult<List<EmployeeLeaveResponse>>> Get([FromQuery] int? employeeId, CancellationToken ct)
        => Ok(await _leaves.GetAsync(_currentUser.CompanyId, employeeId, ct));

    [Authorize(Policy = AppPermissions.EmployeeUpdate)]
    [HttpPost]
    public async Task<ActionResult<EmployeeLeaveResponse>> Add([FromBody] SaveEmployeeLeaveRequest request, CancellationToken ct)
        => Ok(await _leaves.AddAsync(_currentUser.CompanyId, request, ct));

    [Authorize(Policy = AppPermissions.EmployeeUpdate)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _leaves.DeleteAsync(_currentUser.CompanyId, id, ct);
        return NoContent();
    }
}
