using Application.MenuItemRecipes.Commands.Upsert;
using Application.MenuItemRecipes.Dtos;
using Application.MenuItemRecipes.Queries.GetAll;
using Application.MenuItemRecipes.Queries.GetByMenuItemId;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class MenuItemRecipesController : ControllerBase
{
    private readonly IMediator _mediator;

    public MenuItemRecipesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = AppPermissions.MenuItemView)]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllMenuItemRecipeLinesQuery(), cancellationToken);
        return Ok(new { success = true, data = result });
    }

    [Authorize(Policy = AppPermissions.MenuItemView)]
    [HttpGet("menu-items/{menuItemId:int}")]
    public async Task<IActionResult> GetByMenuItemId(
        int menuItemId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMenuItemRecipeByMenuItemIdQuery(menuItemId), cancellationToken);
        return Ok(new { success = true, data = result });
    }

    [Authorize(Policy = AppPermissions.MenuItemView)]
    [HttpGet("by-menu-item/{menuItemId:int}")]
    public async Task<IActionResult> GetByMenuItemIdAlias(
        int menuItemId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMenuItemRecipeByMenuItemIdQuery(menuItemId), cancellationToken);
        return Ok(new { success = true, data = result });
    }

    [Authorize(Policy = AppPermissions.MenuItemUpdate)]
    [HttpPut("menu-items/{menuItemId:int}")]
    public async Task<IActionResult> UpsertByMenuItemId(
        int menuItemId,
        [FromBody] UpsertMenuItemRecipeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpsertMenuItemRecipeCommand(menuItemId, request), cancellationToken);
        return Ok(new { success = true, data = result });
    }

    [Authorize(Policy = AppPermissions.MenuItemUpdate)]
    [HttpPost]
    public async Task<IActionResult> CreateOrUpdate(
        [FromBody] CreateOrUpdateMenuItemRecipeRequest request,
        CancellationToken cancellationToken)
    {
        var payload = new UpsertMenuItemRecipeRequest
        {
            Lines = request.Lines
        };
        var result = await _mediator.Send(new UpsertMenuItemRecipeCommand(request.MenuItemId, payload), cancellationToken);
        return Ok(new { success = true, data = result });
    }
}
