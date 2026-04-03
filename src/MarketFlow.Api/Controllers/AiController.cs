using Microsoft.AspNetCore.Mvc;
using MarketFlow.Api.Dtos;
using MarketFlow.Api.Services;

namespace MarketFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly AiSuggestionService _service;

    public AiController(AiSuggestionService service)
    {
        _service = service;
    }

    [HttpPost("subject-suggestions")]
    public ActionResult<SubjectSuggestionResponse> SuggestSubjectLines([FromBody] SubjectSuggestionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DraftSubject))
            return BadRequest(new { error = "DraftSubject is required." });

        var response = _service.GenerateSubjectSuggestions(request.DraftSubject, request.CampaignContext);
        return Ok(response);
    }

    [HttpGet("send-time/{segmentId?}")]
    public async Task<ActionResult<SendTimeRecommendation>> GetSendTimeRecommendation(int? segmentId = null)
    {
        var recommendation = await _service.GetSendTimeRecommendationAsync(segmentId);
        return Ok(recommendation);
    }
}
