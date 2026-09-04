using Application.ScaleDevice.Commands;
using Application.ScaleDevice.Dtos;
using Application.ScaleDevice.Queries;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ScaleDevicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ScaleDevicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = AppPermissions.ScaleDeviceView)]
    [HttpGet]
    public async Task<ActionResult<List<ScaleDeviceResponse>>> GetAll([FromQuery] int restaurantId)
    {
        var result = await _mediator.Send(new GetAllScaleDevicesQuery(restaurantId));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.ScaleDeviceCreate)]
    [HttpPost]
    public async Task<ActionResult<ScaleDeviceResponse>> Create([FromBody] CreateScaleDeviceRequest request)
    {
        var result = await _mediator.Send(new CreateScaleDeviceCommand(request));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.ScaleDeviceUpdate)]
    [HttpPut]
    public async Task<ActionResult<ScaleDeviceResponse>> Update([FromBody] UpdateScaleDeviceRequest request)
    {
        var result = await _mediator.Send(new UpdateScaleDeviceCommand(request));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.ScaleDeviceDelete)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _mediator.Send(new DeleteScaleDeviceCommand(id));
        return Ok();
    }
}
