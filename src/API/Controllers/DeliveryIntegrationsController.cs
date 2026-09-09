using Application.DeliveryIntegration.Commands;
using Application.DeliveryIntegration.Dtos;
using Application.DeliveryIntegration.Queries;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DeliveryIntegrationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DeliveryIntegrationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = AppPermissions.DeliveryIntegrationView)]
    [HttpGet]
    public async Task<ActionResult<List<DeliveryIntegrationResponse>>> GetAll([FromQuery] int restaurantId)
    {
        var result = await _mediator.Send(new GetAllDeliveryIntegrationsQuery(restaurantId));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.DeliveryIntegrationCreate)]
    [HttpPost]
    public async Task<ActionResult<DeliveryIntegrationResponse>> Create([FromBody] CreateDeliveryIntegrationRequest request)
    {
        var result = await _mediator.Send(new CreateDeliveryIntegrationCommand(request));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.DeliveryIntegrationUpdate)]
    [HttpPut]
    public async Task<ActionResult<DeliveryIntegrationResponse>> Update([FromBody] UpdateDeliveryIntegrationRequest request)
    {
        var result = await _mediator.Send(new UpdateDeliveryIntegrationCommand(request));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.DeliveryIntegrationDelete)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _mediator.Send(new DeleteDeliveryIntegrationCommand(id));
        return Ok();
    }
}
