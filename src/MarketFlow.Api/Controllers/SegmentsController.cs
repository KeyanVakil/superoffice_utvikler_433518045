using Microsoft.AspNetCore.Mvc;
using MarketFlow.Api.Dtos;
using MarketFlow.Api.Services;

namespace MarketFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SegmentsController : ControllerBase
{
    private readonly SegmentService _service;

    public SegmentsController(SegmentService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<SegmentListDto>>> GetSegments()
    {
        var segments = await _service.GetSegmentsAsync();
        return Ok(segments);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SegmentDetailDto>> GetSegment(int id)
    {
        var segment = await _service.GetSegmentAsync(id);
        if (segment is null) return NotFound();
        return Ok(segment);
    }

    [HttpPost]
    public async Task<ActionResult<SegmentDetailDto>> CreateSegment([FromBody] CreateSegmentDto dto)
    {
        var segment = await _service.CreateSegmentAsync(dto);
        return CreatedAtAction(nameof(GetSegment), new { id = segment.Id }, segment);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SegmentDetailDto>> UpdateSegment(int id, [FromBody] CreateSegmentDto dto)
    {
        var segment = await _service.UpdateSegmentAsync(id, dto);
        if (segment is null) return NotFound();
        return Ok(segment);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSegment(int id)
    {
        var deleted = await _service.DeleteSegmentAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpGet("{id}/preview")]
    public async Task<ActionResult<SegmentPreviewDto>> PreviewSegment(int id)
    {
        var preview = await _service.PreviewSegmentAsync(id);
        if (preview is null) return NotFound();
        return Ok(preview);
    }

    [HttpPost("preview")]
    public async Task<ActionResult<SegmentPreviewDto>> PreviewRules([FromBody] CreateSegmentDto dto)
    {
        var preview = await _service.PreviewRulesAsync(dto.Rules ?? new List<SegmentRuleDto>());
        return Ok(preview);
    }
}
