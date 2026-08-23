using Application.Analytics.Dtos;
using Application.Analytics.Queries;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AnalyticsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = AppPermissions.AnalyticsView)]
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardAnalyticsResponse>> GetDashboard(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetDashboardAnalyticsQuery(), cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = AppPermissions.AnalyticsView)]
    [HttpGet("food-cost")]
    public async Task<ActionResult<List<FoodCostResponse>>> GetFoodCost(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFoodCostQuery(), cancellationToken);
        return Ok(result);
    }
}
