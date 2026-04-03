using Microsoft.AspNetCore.Mvc;
using MarketFlow.Api.Dtos;
using MarketFlow.Api.Services;

namespace MarketFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JourneysController : ControllerBase
{
    private readonly JourneyService _service;

    public JourneysController(JourneyService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<JourneyListDto>>> GetJourneys()
    {
        var journeys = await _service.GetJourneysAsync();
        return Ok(journeys);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<JourneyDetailDto>> GetJourney(int id)
    {
        var journey = await _service.GetJourneyAsync(id);
        if (journey is null) return NotFound();
        return Ok(journey);
    }

    [HttpPost]
    public async Task<ActionResult<JourneyDetailDto>> CreateJourney([FromBody] CreateJourneyDto dto)
    {
        var journey = await _service.CreateJourneyAsync(dto);
        return CreatedAtAction(nameof(GetJourney), new { id = journey.Id }, journey);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<JourneyDetailDto>> UpdateJourney(int id, [FromBody] CreateJourneyDto dto)
    {
        var journey = await _service.UpdateJourneyAsync(id, dto);
        if (journey is null) return NotFound();
        return Ok(journey);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteJourney(int id)
    {
        var deleted = await _service.DeleteJourneyAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPost("{id}/activate")]
    public async Task<ActionResult<JourneyDetailDto>> Activate(int id)
    {
        var journey = await _service.ActivateJourneyAsync(id);
        if (journey is null) return NotFound();
        return Ok(journey);
    }

    [HttpPost("{id}/deactivate")]
    public async Task<ActionResult<JourneyDetailDto>> Deactivate(int id)
    {
        var journey = await _service.DeactivateJourneyAsync(id);
        if (journey is null) return NotFound();
        return Ok(journey);
    }

    [HttpPost("{journeyId}/enroll/{contactId}")]
    public async Task<IActionResult> EnrollContact(int journeyId, int contactId)
    {
        var enrollment = await _service.EnrollContactAsync(journeyId, contactId);
        if (enrollment is null) return NotFound();
        return Ok(new { enrollment.Id, enrollment.Status, enrollment.EnrolledAt });
    }

    [HttpPost("enrollments/{enrollmentId}/process")]
    public async Task<IActionResult> ProcessStep(int enrollmentId)
    {
        var processed = await _service.ProcessStepAsync(enrollmentId);
        if (!processed) return BadRequest(new { error = "Step could not be processed. Enrollment may be completed or still waiting." });
        return Ok(new { message = "Step processed successfully." });
    }

    [HttpGet("{id}/stats")]
    public async Task<ActionResult<JourneyStatsDto>> GetJourneyStats(int id)
    {
        var stats = await _service.GetJourneyStatsAsync(id);
        if (stats is null) return NotFound();
        return Ok(stats);
    }
}
