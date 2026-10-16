using API.Middlewares;
using Application.License;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public DevicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>A restaurant device exchanges the SuperAdmin's one-time code for its device key.</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<RegisterDeviceResponse>> Register([FromBody] RegisterDeviceRequest request) =>
        Ok(await _mediator.Send(new RegisterDeviceCommand(request)));
}
