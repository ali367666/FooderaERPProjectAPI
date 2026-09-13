using Application.Restaurant.Commands.Create;
using Application.Restaurant.Dtos.Request;
using Application.Restaurant.Queries;
using Application.RestaurantSettings.Commands.SetModules;
using Application.RestaurantSettings.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.Constants;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class RestaurantController : ControllerBase
{
    private readonly IMediator _mediator;

    public RestaurantController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = AppPermissions.RestaurantCreate)]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateRestaurantRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateRestaurantCommand(request);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.RestaurantView)]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var query = new GetRestaurantByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.RestaurantView)]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? companyId, CancellationToken cancellationToken)
    {
        var query = new GetAllRestaurantsQuery(companyId);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.RestaurantView)]
    [HttpGet("company/{companyId:int}")]
    public async Task<IActionResult> GetByCompanyId(
        [FromRoute] int companyId,
        CancellationToken cancellationToken)
    {
        var query = new GetRestaurantsByCompanyIdQuery(companyId);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.RestaurantUpdate)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] UpdateRestaurantRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateRestaurantCommand(id, request);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.RestaurantDelete)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteRestaurantCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Filial-səviyyəli modullar — yalnız platforma səviyyəsində şirkətləri idarə edən istifadəçi
    /// (Company.Update icazəsi) üçündür, müştərinin öz admin panelindən dəyişdirilə bilmir. Yalnız
    /// ana şirkət "İstirahət Kompleksi" rejimindədirsə mənalıdır.
    /// </summary>
    [Authorize(Policy = AppPermissions.CompanyUpdate)]
    [HttpGet("{id:int}/modules")]
    public async Task<IActionResult> GetModules([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetRestaurantModulesQuery(id), cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [Authorize(Policy = AppPermissions.CompanyUpdate)]
    [HttpPut("{id:int}/modules")]
    public async Task<IActionResult> SetModules(
        [FromRoute] int id,
        [FromBody] SetRestaurantModulesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SetRestaurantModulesCommand(id, request), cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}