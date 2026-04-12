using Application.Order.Dtos.Request;
using Application.OrderLines.Commands.Add;
using Application.OrderLines.Commands.Delete;
using Application.OrderLines.Commands.Update;
using Application.Orders.Commands.Create;
using Application.Orders.Commands.Delete;
using Application.Orders.Commands.Update;
using Application.Orders.Dtos;
using Application.Orders.Queries.GetAll;
using Application.Orders.Queries.GetById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FooderaERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create([FromBody] CreateOrderRequest request)
    {
        var result = await _mediator.Send(new CreateOrderCommand(request));
        return Ok(result);
    }

    [HttpPut]
    public async Task<ActionResult<OrderResponse>> Update([FromBody] UpdateOrderRequest request)
    {
        var result = await _mediator.Send(new UpdateOrderCommand(request));
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<string>> Delete(int id)
    {
        var result = await _mediator.Send(new DeleteOrderCommand(id));
        return Ok(result);
    }

    [HttpPost("lines")]
    public async Task<ActionResult<OrderResponse>> AddLine([FromBody] AddOrderLineRequest request)
    {
        var result = await _mediator.Send(new AddOrderLineCommand(request));
        return Ok(result);
    }

    [HttpPut("lines")]
    public async Task<ActionResult<OrderResponse>> UpdateLine([FromBody] UpdateOrderLineRequest request)
    {
        var result = await _mediator.Send(new UpdateOrderLineCommand(request));
        return Ok(result);
    }

    [HttpDelete("lines/{id}")]
    public async Task<ActionResult<OrderResponse>> DeleteLine(int id)
    {
        var result = await _mediator.Send(new DeleteOrderLineCommand(id));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponse>> GetById(int id)
    {
        var result = await _mediator.Send(new GetOrderByIdQuery(id));
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<List<OrderResponse>>> GetAll()
    {
        var result = await _mediator.Send(new GetAllOrdersQuery());
        return Ok(result);
    }
}