using Application.Workstation.Commands.Create;
using Application.Workstation.Commands.Delete;
using Application.Workstation.Commands.Update;
using Application.Workstation.Dtos.Request;
using Application.Workstation.Queries;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/workstations")]
[Authorize]
public class WorkstationsController(IMediator mediator) : BaseController(mediator)
{
    [Authorize(Policy = AppPermissions.WorkstationView)]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? restaurantId,
        CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new GetAllWorkstationsQuery(restaurantId), cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = AppPermissions.WorkstationCreate)]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateWorkstationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new CreateWorkstationCommand(request), cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [Authorize(Policy = AppPermissions.WorkstationUpdate)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] UpdateWorkstationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new UpdateWorkstationCommand(id, request), cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [Authorize(Policy = AppPermissions.WorkstationDelete)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new DeleteWorkstationCommand(id), cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }
}
