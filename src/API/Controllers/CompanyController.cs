using Application.Company.Commands.Create;
using Application.Company.Commands.Delete;
using Application.Company.Commands.Update;
using Application.Company.Dtos.Request;
using Application.Company.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
[Route("api/[controller]")]
[ApiController]
public class CompanyController(IMediator mediator) : BaseController(mediator)
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateCompanyCommand command, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var company = await Mediator.Send(new GetCompanyByIdQuery(id), cancellationToken);

        if (company == null)
            return NotFound();

        return Ok(company);
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new DeleteCompanyCommand(id), cancellationToken);
        return Ok(response);
    }
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var companies = await Mediator.Send(new GetCompaniesQuery(), cancellationToken);
        return Ok(companies);
    }
    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCompanyRequest dto, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new UpdateCompanyCommand(id, dto), cancellationToken);
        return Ok(response);
    }
}
