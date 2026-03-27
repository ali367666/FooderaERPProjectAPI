using Application.StockCategory.Commands.Create;
using Application.StockCategory.Commands.Delete;
using Application.StockCategory.Commands.Update;
using Application.StockCategory.Dtos.Request;
using Application.StockCategory.Queries.GetAll;
using Application.StockCategory.Queries.GetByCompanyId;
using Application.StockCategory.Queries.GetById;
using Application.StockCategory.Queries.GetChildrenByParentId;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class StockCategoryController : ControllerBase
{
    private readonly IMediator _mediator;

    public StockCategoryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = "StockCategoryCreate")]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateStockCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateStockCategoryCommand(request);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = "StockCategoryView")]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var query = new GetStockCategoryByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [Authorize(Policy = "StockCategoryView")]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetAllStockCategoriesRequest request,
        CancellationToken cancellationToken)
    {
        var query = new GetAllStockCategoriesQuery(request);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = "StockCategoryView")]
    [HttpGet("company/{companyId:int}")]
    public async Task<IActionResult> GetByCompanyId(int companyId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetStockCategoriesByCompanyIdQuery(companyId), cancellationToken);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
    [Authorize(Policy = "StockCategoryView")]
    [HttpGet("children/{parentId:int}")]
    public async Task<IActionResult> GetChildren(
        [FromRoute] int parentId,
        CancellationToken cancellationToken)
    {
        var query = new GetChildrenByParentIdQuery(parentId);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [Authorize(Policy = "StockCategoryUpdate")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] UpdateStockCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateStockCategoryCommand(id, request);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = "StockCategoryDelete")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteStockCategoryCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}