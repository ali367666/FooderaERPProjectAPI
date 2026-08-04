using Application.Discounts.Commands.Create;
using Application.Discounts.Commands.Delete;
using Application.Discounts.Commands.Update;
using Application.Discounts.Dtos;
using Application.Discounts.Queries;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DiscountsController : ControllerBase
{
    private readonly IMediator _mediator;
    public DiscountsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Policy = AppPermissions.DiscountView)]
    public async Task<ActionResult<List<DiscountResponse>>> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAllDiscountsQuery(), ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = AppPermissions.DiscountCreate)]
    public async Task<ActionResult<DiscountResponse>> Create(
        [FromBody] CreateDiscountRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateDiscountCommand { Request = request }, ct);
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AppPermissions.DiscountUpdate)]
    public async Task<ActionResult<DiscountResponse>> Update(
        int id, [FromBody] UpdateDiscountRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateDiscountCommand { Id = id, Request = request }, ct);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AppPermissions.DiscountDelete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteDiscountCommand { Id = id }, ct);
        return NoContent();
    }
}
