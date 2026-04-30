using Application.Common.Responce;
using Application.Departments.Commands.Create;
using Application.Departments.Commands.Delete;
using Application.Departments.Commands.Update;
using Application.Departments.Dtos;
using Application.Departments.Queries.GetAll;
using Application.Departments.Queries.GetById;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Foodera.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DepartmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DepartmentsController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [Authorize(Policy = AppPermissions.DepartmentCreate)]
    [HttpPost]
    public async Task<ActionResult<BaseResponse<DepartmentResponse>>> Create(
        [FromBody] CreateDepartmentRequest request)
    {
        var result = await _mediator.Send(new CreateDepartmentCommand(request));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
    [Authorize(Policy = AppPermissions.DepartmentUpdate)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<BaseResponse<DepartmentResponse>>> Update(
        int id,
        [FromQuery] int companyId,
        [FromBody] UpdateDepartmentRequest request)
    {
        var result = await _mediator.Send(new UpdateDepartmentCommand(id, companyId, request));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
    [Authorize(Policy = AppPermissions.DepartmentView)]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BaseResponse<DepartmentResponse>>> GetById(
        int id,
        [FromQuery] int companyId)
    {
        var result = await _mediator.Send(new GetDepartmentByIdQuery(id, companyId));

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<BaseResponse<List<DepartmentResponse>>>> GetAll(
        [FromQuery] int? companyId)
    {
        var hasDepartmentViewPermission = User.Claims.Any(
            x => x.Type == "Permission" && x.Value == AppPermissions.DepartmentView);

        if (!hasDepartmentViewPermission)
            return Forbid();

        var companyIdFromClaim = User.FindFirst("companyId")?.Value;
        var effectiveCompanyId = companyId;

        if (effectiveCompanyId is null && int.TryParse(companyIdFromClaim, out var parsedCompanyId))
            effectiveCompanyId = parsedCompanyId;

        if (effectiveCompanyId is null || effectiveCompanyId <= 0)
            return BadRequest(BaseResponse<List<DepartmentResponse>>.Fail("Valid companyId is required."));

        var result = await _mediator.Send(new GetAllDepartmentsQuery(effectiveCompanyId.Value));
        return Ok(result);
    }
    [Authorize(Policy = AppPermissions.DepartmentDelete)]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<BaseResponse>> Delete(
        int id,
        [FromQuery] int companyId)
    {
        var result = await _mediator.Send(new DeleteDepartmentCommand(id, companyId));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}