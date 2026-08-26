using Application.RestaurantSection.Commands;
using Application.RestaurantSection.Dtos;
using Application.RestaurantSection.Queries;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RestaurantSectionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RestaurantSectionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = AppPermissions.RestaurantSectionView)]
    [HttpGet]
    public async Task<ActionResult<List<RestaurantSectionResponse>>> GetAll([FromQuery] int restaurantId)
    {
        var result = await _mediator.Send(new GetAllRestaurantSectionsQuery(restaurantId));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.RestaurantSectionCreate)]
    [HttpPost]
    public async Task<ActionResult<RestaurantSectionResponse>> Create([FromBody] CreateRestaurantSectionRequest request)
    {
        var result = await _mediator.Send(new CreateRestaurantSectionCommand(request));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.RestaurantSectionUpdate)]
    [HttpPut]
    public async Task<ActionResult<RestaurantSectionResponse>> Update([FromBody] UpdateRestaurantSectionRequest request)
    {
        var result = await _mediator.Send(new UpdateRestaurantSectionCommand(request));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.RestaurantSectionDelete)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _mediator.Send(new DeleteRestaurantSectionCommand(id));
        return Ok();
    }
}
