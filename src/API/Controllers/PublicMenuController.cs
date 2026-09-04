using Application.PublicMenu.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[Route("api/public-menu")]
[ApiController]
[AllowAnonymous]
public class PublicMenuController : ControllerBase
{
    private readonly IMediator _mediator;

    public PublicMenuController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{restaurantId}")]
    public async Task<IActionResult> Get(int restaurantId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPublicMenuQuery(restaurantId), cancellationToken);
        if (result is null)
            return NotFound();

        return Ok(result);
    }
}
