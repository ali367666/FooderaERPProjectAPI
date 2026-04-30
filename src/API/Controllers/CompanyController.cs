using Application.Company.Commands.Create;
using Application.Company.Commands.Delete;
using Application.Company.Commands.Update;
using Application.Company.Dtos.Request;
using Application.Company.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.Constants;

namespace API.Controllers;

[Route("api/companies")]
[ApiController]
[Authorize]
public class CompanyController(IMediator mediator) : BaseController(mediator)
{
    [Authorize(Policy = AppPermissions.CompanyCreate)]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new CreateCompanyCommand(request), cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [Authorize(Policy = AppPermissions.CompanyView)]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new GetCompanyByIdQuery(id), cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [Authorize(Policy = AppPermissions.CompanyDelete)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new DeleteCompanyCommand(id), cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [Authorize(Policy = AppPermissions.CompanyView)]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var companies = await Mediator.Send(new GetCompaniesQuery(), cancellationToken);
        return Ok(companies);
    }

    [Authorize(Policy = AppPermissions.CompanyUpdate)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] UpdateCompanyRequest dto,
        CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new UpdateCompanyCommand(id, dto), cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }
}