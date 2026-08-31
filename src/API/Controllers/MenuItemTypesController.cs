using Application.MenuItemType.Commands;
using Application.MenuItemType.Dtos;
using Application.MenuItemType.Queries;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MenuItemTypesController : ControllerBase
{
    private readonly IMediator _mediator;

    public MenuItemTypesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = AppPermissions.MenuItemView)]
    [HttpGet]
    public async Task<ActionResult<List<MenuItemTypeResponse>>> GetAll()
    {
        var result = await _mediator.Send(new GetAllMenuItemTypesQuery());
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.MenuItemCreate)]
    [HttpPost]
    public async Task<ActionResult<MenuItemTypeResponse>> Create([FromBody] CreateMenuItemTypeRequest request)
    {
        var result = await _mediator.Send(new CreateMenuItemTypeCommand(request));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.MenuItemUpdate)]
    [HttpPut]
    public async Task<ActionResult<MenuItemTypeResponse>> Update([FromBody] UpdateMenuItemTypeRequest request)
    {
        var result = await _mediator.Send(new UpdateMenuItemTypeCommand(request));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.MenuItemDelete)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _mediator.Send(new DeleteMenuItemTypeCommand(id));
        return Ok();
    }
}
