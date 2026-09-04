using Application.FiscalDevice.Commands;
using Application.FiscalDevice.Dtos;
using Application.FiscalDevice.Queries;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FiscalDevicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public FiscalDevicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = AppPermissions.FiscalDeviceView)]
    [HttpGet]
    public async Task<ActionResult<List<FiscalDeviceResponse>>> GetAll([FromQuery] int restaurantId)
    {
        var result = await _mediator.Send(new GetAllFiscalDevicesQuery(restaurantId));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.FiscalDeviceCreate)]
    [HttpPost]
    public async Task<ActionResult<FiscalDeviceResponse>> Create([FromBody] CreateFiscalDeviceRequest request)
    {
        var result = await _mediator.Send(new CreateFiscalDeviceCommand(request));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.FiscalDeviceUpdate)]
    [HttpPut]
    public async Task<ActionResult<FiscalDeviceResponse>> Update([FromBody] UpdateFiscalDeviceRequest request)
    {
        var result = await _mediator.Send(new UpdateFiscalDeviceCommand(request));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.FiscalDeviceDelete)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _mediator.Send(new DeleteFiscalDeviceCommand(id));
        return Ok();
    }
}
