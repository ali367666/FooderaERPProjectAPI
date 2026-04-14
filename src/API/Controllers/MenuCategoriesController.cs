using Application.MenuCategories.Commands.Create;
using Application.MenuCategories.Commands.Delete;
using Application.MenuCategories.Commands.Update;
using Application.MenuCategories.Dtos;
using Application.MenuCategories.Queries.GetAll;
using Application.MenuCategories.Queries.GetById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MenuCategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public MenuCategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [Authorize(Policy = "MenuCategoryCreate")]
    [HttpPost]
    public async Task<IActionResult> Create(
    [FromBody] CreateMenuCategoryRequest request,
    CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            new CreateMenuCategoryCommand(request),
            cancellationToken);

        return Ok(new
        {
            Id = id,
            Message = "Menu kateqoriyası uğurla yaradıldı."
        });
    }

    [Authorize(Policy = "MenuCategoryUpdate")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        int id,
        [FromQuery] int companyId,
        [FromBody] UpdateMenuCategoryRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateMenuCategoryCommand(id, companyId, request),
            cancellationToken);

        return NoContent();
    }
    [Authorize(Policy = "MenuCategoryDelete")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(
        int id,
        [FromQuery] int companyId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new DeleteMenuCategoryCommand(id, companyId),
            cancellationToken);

        return NoContent();
    }
    [Authorize(Policy = "MenuCategoryView")]
    [HttpGet("{id}")]
    public async Task<ActionResult<MenuCategoryResponse>> GetById(
        int id,
        [FromQuery] int companyId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetMenuCategoryByIdQuery(id, companyId),
            cancellationToken);

        return Ok(result);
    }

    [Authorize(Policy = "MenuCategoryView")]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllMenuCategoriesQuery(), cancellationToken);
        return Ok(result);
    }
}