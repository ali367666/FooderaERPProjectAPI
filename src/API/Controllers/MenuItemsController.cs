using Application.MenuItems.Commands.Create;
using Application.MenuItems.Commands.Delete;
using Application.MenuItems.Commands.Update;
using Application.MenuItems.Dtos;
using Application.MenuItems.Queries.GetAll;
using Application.MenuItems.Queries.GetById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.Constants;

namespace WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MenuItemsController : ControllerBase
{
    private readonly IMediator _mediator;

    public MenuItemsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = AppPermissions.MenuItemCreate)]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateMenuItemRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            new CreateMenuItemCommand(request),
            cancellationToken);

        return Ok(new
        {
            Id = id,
            Message = "Menu məhsulu uğurla yaradıldı."
        });
    }
    [Authorize(Policy = AppPermissions.MenuItemUpdate)]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateMenuItemRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateMenuItemCommand(id, request),
            cancellationToken);

        return NoContent();
    }
    [Authorize(Policy = AppPermissions.MenuItemDelete)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new DeleteMenuItemCommand(id),
            cancellationToken);

        return NoContent();
    }
    [Authorize(Policy = AppPermissions.MenuItemView)]
    [HttpGet("{id}")]
    public async Task<ActionResult<MenuItemResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetMenuItemByIdQuery(id),
            cancellationToken);

        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.MenuItemView)]
    [HttpGet]
    public async Task<ActionResult<List<MenuItemResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetAllMenuItemsQuery(),
            cancellationToken);

        return Ok(result);
    }
}