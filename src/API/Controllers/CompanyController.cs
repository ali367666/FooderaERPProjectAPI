using Application.Company.Commands.Create;
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
}
