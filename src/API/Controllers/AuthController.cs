using Application.Auth.Commands.Login;
using Application.Auth.Dtos;
using Application.Auth.Dtos.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var command = new LoginCommand
        {
            Request = request,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    
}