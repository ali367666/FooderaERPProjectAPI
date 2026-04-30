using Application.Order.Dtos.Request;
using Application.Order.Commands.Serve;
using Application.OrderLines.Commands.Add;
using Application.Orders.Commands.Pay;
using Application.Orders.Dtos.Request;
using Application.Orders.Commands.Cancel;
using Application.Orders.Commands.Complete;
using Application.OrderLines.Commands.Delete;
using Application.OrderLines.Commands.Update;
using Application.Orders.Commands.Create;
using Application.Orders.Commands.Delete;
using Application.Orders.Commands.Start;
using Application.Orders.Commands.Submit;
using Application.Orders.Commands.Update;
using Application.Orders.Dtos;
using Application.Orders.Queries.GetAll;
using Application.Orders.Queries.GetById;
using Application.Orders.Queries.GetReceipt;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.Constants;

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

    [Authorize(Policy = AppPermissions.OrdersCreate)]
    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create([FromBody] CreateOrderRequest request)
    {
        var result = await _mediator.Send(new CreateOrderCommand(request));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.OrdersUpdate)]
    [HttpPut]
    public async Task<ActionResult<OrderResponse>> Update([FromBody] UpdateOrderRequest request)
    {
        var result = await _mediator.Send(new UpdateOrderCommand(request));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.OrdersDelete)]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<string>> Delete(int id)
    {
        var result = await _mediator.Send(new DeleteOrderCommand(id));
        return Ok(result);
    }
    [Authorize(Policy = AppPermissions.OrdersAdd)]
    [HttpPost("lines")]
    public async Task<ActionResult<OrderResponse>> AddLine([FromBody] AddOrderLineRequest request)
    {
        var result = await _mediator.Send(new AddOrderLineCommand(request));
        return Ok(result);
    }
    [Authorize(Policy = AppPermissions.OrdersUpdate)]
    [HttpPut("lines")]
    public async Task<ActionResult<OrderResponse>> UpdateLine([FromBody] UpdateOrderLineRequest request)
    {
        var result = await _mediator.Send(new UpdateOrderLineCommand(request));
        return Ok(result);
    }
    [Authorize(Policy = AppPermissions.OrdersDelete)]
    [HttpDelete("lines/{id}")]
    public async Task<ActionResult<OrderResponse>> DeleteLine(int id)
    {
        var result = await _mediator.Send(new DeleteOrderLineCommand(id));
        return Ok(result);
    }
    [Authorize(Policy = AppPermissions.OrdersView)]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponse>> GetById(int id)
    {
        var result = await _mediator.Send(new GetOrderByIdQuery(id));
        return Ok(result);
    }
    [Authorize(Policy = AppPermissions.OrdersView)]
    [HttpGet]
    public async Task<ActionResult<List<OrderResponse>>> GetAll()
    {
        var result = await _mediator.Send(new GetAllOrdersQuery());
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.OrdersUpdate)]
    [HttpPost("{id:int}/submit")]
    public async Task<ActionResult<OrderResponse>> Submit(int id)
    {
        var result = await _mediator.Send(new SubmitOrderCommand(id));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.OrdersUpdate)]
    [HttpPost("{id:int}/start")]
    public async Task<ActionResult<OrderResponse>> Start(int id)
    {
        var result = await _mediator.Send(new StartOrderCommand(id));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.OrdersUpdate)]
    [HttpPost("{id:int}/complete")]
    public async Task<ActionResult<OrderResponse>> Complete(int id)
    {
        var result = await _mediator.Send(new CompleteOrderCommand(id));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.OrdersUpdate)]
    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<OrderResponse>> Cancel(int id)
    {
        var result = await _mediator.Send(new CancelOrderCommand(id));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.OrdersServe)]
    [HttpPut("{id}/serve")]
    public async Task<ActionResult<Application.Common.Responce.BaseResponse>> Serve(int id)
    {
        var result = await _mediator.Send(new ServeOrderCommand(id));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.OrdersPay)]
    [HttpPut("{id:int}/pay")]
    public async Task<ActionResult<OrderResponse>> Pay(int id, [FromBody] PayOrderRequest request)
    {
        var result = await _mediator.Send(new PayOrderCommand(id, request));
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.OrdersView)]
    [HttpGet("{id:int}/receipt")]
    public async Task<ActionResult<OrderReceiptResponse>> GetReceipt(int id)
    {
        var result = await _mediator.Send(new GetOrderReceiptQuery(id));
        return Ok(result);
    }
}