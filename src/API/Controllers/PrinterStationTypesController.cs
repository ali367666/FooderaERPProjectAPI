using Application.PrinterStationType.Commands;
using Application.PrinterStationType.Dtos;
using Application.PrinterStationType.Queries;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PrinterStationTypesController : ControllerBase
{
    private readonly IMediator _mediator;

    public PrinterStationTypesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = AppPermissions.PrinterView)]
    [HttpGet]
    public async Task<ActionResult<List<PrinterStationTypeResponse>>> GetAll()
    {
        var result = await _mediator.Send(new GetAllPrinterStationTypesQuery());
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.PrinterCreate)]
    [HttpPost]
    public async Task<ActionResult<PrinterStationTypeResponse>> Create([FromBody] CreatePrinterStationTypeRequest request)
    {
        var result = await _mediator.Send(new CreatePrinterStationTypeCommand(request));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.PrinterUpdate)]
    [HttpPut]
    public async Task<ActionResult<PrinterStationTypeResponse>> Update([FromBody] UpdatePrinterStationTypeRequest request)
    {
        var result = await _mediator.Send(new UpdatePrinterStationTypeCommand(request));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.PrinterDelete)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _mediator.Send(new DeletePrinterStationTypeCommand(id));
        return Ok();
    }
}
