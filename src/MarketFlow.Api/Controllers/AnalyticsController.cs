using Microsoft.AspNetCore.Mvc;
using MarketFlow.Api.Dtos;
using MarketFlow.Api.Services;

namespace MarketFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly AnalyticsService _service;

    public AnalyticsController(AnalyticsService service)
    {
        _service = service;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<OverviewDto>> GetOverview([FromQuery] int days = 30)
    {
        var overview = await _service.GetOverviewAsync(days);
        return Ok(overview);
    }

    [HttpGet("engagement")]
    public async Task<ActionResult<List<EngagementTrendDto>>> GetEngagementTrends([FromQuery] int days = 30)
    {
        var trends = await _service.GetEngagementTrendsAsync(days);
        return Ok(trends);
    }
}
