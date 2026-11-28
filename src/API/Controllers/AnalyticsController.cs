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

    [Authorize(Policy = AppPermissions.AnalyticsView)]
    [HttpGet("food-cost/summary")]
    public async Task<ActionResult<FoodCostSummaryResponse>> GetFoodCostSummary(CancellationToken cancellationToken)
    {
        var items = await _mediator.Send(new GetFoodCostQuery(), cancellationToken);
        var costed = items.Where(x => x.HasRecipe).ToList();
        return Ok(new FoodCostSummaryResponse
        {
            AverageFoodCostPercent = costed.Count == 0 ? 0 : Math.Round(costed.Average(x => x.FoodCostPercentage), 1),
            AverageMarginPercent = costed.Count == 0 ? 0 : Math.Round(costed.Average(x => x.GrossProfitMargin), 1),
            HighCostCount = costed.Count(x => x.FoodCostPercentage > 40),
            WithoutRecipeCount = items.Count - costed.Count,
            MissingCostCount = costed.Count(x => x.HasMissingCost)
        });
    }

    [Authorize(Policy = AppPermissions.PosZReport)]
    [HttpGet("z-report")]
    public async Task<ActionResult<SalesReportResponse>> GetZReport(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSalesReportQuery(from, to), cancellationToken);
        return Ok(result);
    }
}
