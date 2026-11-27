using Application.Search.Dtos;
using Application.Search.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly IMediator _mediator;

    public SearchController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// The dashboard sidebar search over the data: products, categories, employees, counterparties,
    /// branches and tables (up to 5 of each) that match every typed word. Only kinds the user may view are searched.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<GlobalSearchResult>>> Search(
        [FromQuery] string? q,
        [FromQuery] int? companyId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GlobalSearchQuery(q, companyId), cancellationToken);
        return Ok(result);
    }
}
