using Application.Common.Responce;
using Application.Employees.Commands.Create;
using Application.Employees.Commands.Delete;
using Application.Employees.Commands.Update;
using Application.Employees.Dtos;
using Application.Employees.Queries.GetAll;
using Application.Employees.Queries.GetById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IMediator _mediator;

    public EmployeesController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [Authorize(Policy = "EmployeeCreate")]
    [HttpPost]
    public async Task<ActionResult<BaseResponse<int>>> Create([FromBody] CreateEmployeeRequest request)
    {
        var result = await _mediator.Send(new CreateEmployeeCommand(request));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
    [Authorize(Policy = "EmployeeUpdate")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<BaseResponse>> Update(int id, [FromBody] UpdateEmployeeRequest request)
    {
        var result = await _mediator.Send(new UpdateEmployeeCommand(id, request));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
    [Authorize(Policy = "EmployeeDelete")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<BaseResponse>> Delete(int id)
    {
        var result = await _mediator.Send(new DeleteEmployeeCommand(id));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
    [Authorize(Policy = "EmployeeView")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BaseResponse<EmployeeResponse>>> GetById(int id)
    {
        var result = await _mediator.Send(new GetEmployeeByIdQuery(id));

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
    [Authorize(Policy = "EmployeeView")]
    [HttpGet]
    public async Task<ActionResult<BaseResponse<List<EmployeeResponse>>>> GetAll()
    {
        var result = await _mediator.Send(new GetAllEmployeesQuery());

        return Ok(result);
    }
}