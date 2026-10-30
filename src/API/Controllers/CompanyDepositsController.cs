using Application.CompanyDeposits;
using Application.Common.Responce;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FooderaERP.Api.Controllers;

/// <summary>The money the company put in: the amount it starts working with, and any later deposits.</summary>
[ApiController]
[Route("api/company-deposits")]
public class CompanyDepositsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CompanyDepositsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>The opening deposit, the total, and every entry with its date and who recorded it.</summary>
    [Authorize(Policy = AppPermissions.CompanyDepositView)]
    [HttpGet]
    public async Task<ActionResult<CompanyDepositSummaryResponse>> Get()
        => Ok(await _mediator.Send(new GetCompanyDepositsQuery()));

    /// <summary>Records a deposit; the company's first one is its opening deposit.</summary>
    [Authorize(Policy = AppPermissions.CompanyDepositCreate)]
    [HttpPost]
    public async Task<ActionResult<BaseResponse>> Add([FromBody] AddCompanyDepositRequest request)
    {
        var result = await _mediator.Send(new AddCompanyDepositCommand(request));
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
