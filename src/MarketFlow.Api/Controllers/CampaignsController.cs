using Microsoft.AspNetCore.Mvc;
using MarketFlow.Api.Dtos;
using MarketFlow.Api.Services;

namespace MarketFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CampaignsController : ControllerBase
{
    private readonly CampaignService _service;

    public CampaignsController(CampaignService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<CampaignListDto>>> GetCampaigns()
    {
        var campaigns = await _service.GetCampaignsAsync();
        return Ok(campaigns);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CampaignDetailDto>> GetCampaign(int id)
    {
        var campaign = await _service.GetCampaignAsync(id);
        if (campaign is null) return NotFound();
        return Ok(campaign);
    }

    [HttpPost]
    public async Task<ActionResult<CampaignDetailDto>> CreateCampaign([FromBody] CreateCampaignDto dto)
    {
        var campaign = await _service.CreateCampaignAsync(dto);
        return CreatedAtAction(nameof(GetCampaign), new { id = campaign.Id }, campaign);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CampaignDetailDto>> UpdateCampaign(int id, [FromBody] CreateCampaignDto dto)
    {
        try
        {
            var campaign = await _service.UpdateCampaignAsync(id, dto);
            if (campaign is null) return NotFound();
            return Ok(campaign);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCampaign(int id)
    {
        var deleted = await _service.DeleteCampaignAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPost("{id}/send")]
    public async Task<ActionResult<CampaignDetailDto>> SendCampaign(int id)
    {
        try
        {
            var campaign = await _service.SendCampaignAsync(id);
            if (campaign is null) return NotFound();
            return Ok(campaign);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}/stats")]
    public async Task<ActionResult<CampaignStatsDto>> GetCampaignStats(int id)
    {
        var stats = await _service.GetCampaignStatsAsync(id);
        if (stats is null) return NotFound();
        return Ok(stats);
    }
}
