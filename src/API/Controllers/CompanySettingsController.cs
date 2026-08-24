using Application.CompanySettings.Commands.Update;
using Application.CompanySettings.Dtos;
using Application.CompanySettings.Queries;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/company-settings")]
[ApiController]
[Authorize]
public class CompanySettingsController(IMediator mediator) : BaseController(mediator)
{
    [Authorize(Policy = AppPermissions.CompanySettingsView)]
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new GetCompanySettingsQuery(), cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [AllowAnonymous]
    [HttpGet("branding")]
    public async Task<IActionResult> GetBranding([FromQuery] int companyId, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new GetCompanySettingsBrandingQuery(companyId), cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [Authorize(Policy = AppPermissions.CompanySettingsUpdate)]
    [HttpPut]
    public async Task<IActionResult> Update(
        [FromBody] UpdateCompanySettingsRequest request,
        CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new UpdateCompanySettingsCommand(request), cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }
}
