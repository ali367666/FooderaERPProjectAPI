using Application.Common.Responce;
using Application.Departments.Commands.Create;
using Application.Departments.Commands.Delete;
using Application.Departments.Commands.Update;
using Application.Departments.Dtos;
using Application.Departments.Queries.GetAll;
using Application.Departments.Queries.GetById;
using MediatR;
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

    [HttpPost]
    public async Task<ActionResult<BaseResponse<DepartmentResponse>>> Create(
        [FromBody] CreateDepartmentRequest request)
    {
        var result = await _mediator.Send(new CreateDepartmentCommand(request));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

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

    [HttpGet]
    public async Task<ActionResult<BaseResponse<List<DepartmentResponse>>>> GetAll(
        [FromQuery] int companyId)
    {
        var result = await _mediator.Send(new GetAllDepartmentsQuery(companyId));
        return Ok(result);
    }

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