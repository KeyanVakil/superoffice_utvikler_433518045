using Microsoft.EntityFrameworkCore;
using MarketFlow.Api.Data;
using MarketFlow.Api.Dtos;

namespace MarketFlow.Api.Services;

public class AiSuggestionService
{
    private readonly AppDbContext _db;
    private readonly SegmentService _segmentService;

    public AiSuggestionService(AppDbContext db, SegmentService segmentService)
    {
        _db = db;
        _segmentService = segmentService;
    }

    public SubjectSuggestionResponse GenerateSubjectSuggestions(string draftSubject, string? campaignContext)
    {
        var suggestions = new List<SubjectSuggestion>();
        var draft = draftSubject.Trim();

        // Suggestion 1: Add personalization if missing
        if (!draft.Contains("{{firstName}}", StringComparison.OrdinalIgnoreCase))
        {
            suggestions.Add(new SubjectSuggestion(
                $"{{{{firstName}}}}, {char.ToLower(draft[0])}{draft[1..]}",
                "Adding personalization with the recipient's first name increases open rates by up to 26%."));
        }
        else
        {
            suggestions.Add(new SubjectSuggestion(
                draft.Replace("{{firstName}}", "{{firstName}} {{lastName}}"),
                "Using full name personalization can feel more professional and personal."));
        }

        // Suggestion 2: Numbered list version
        var numberedVersion = draft.Length > 40
            ? $"5 key takeaways: {draft[..Math.Min(40, draft.Length)]}..."
            : $"3 reasons to read: {draft}";
        suggestions.Add(new SubjectSuggestion(
            numberedVersion,
            "Subject lines with numbers get 36% higher open rates. Lists set clear expectations."));

        // Suggestion 3: Urgency / curiosity gap
        if (!draft.Contains("today", StringComparison.OrdinalIgnoreCase) &&
            !draft.Contains("now", StringComparison.OrdinalIgnoreCase) &&
            !draft.Contains("urgent", StringComparison.OrdinalIgnoreCase))
        {
            var urgentVersion = draft.Length > 60
                ? $"{draft[..55]}... (this week only)"
                : $"{draft} — don't miss out this week";
            suggestions.Add(new SubjectSuggestion(
                urgentVersion,
                "Adding time-sensitive language creates urgency and can boost open rates by 22%."));
        }
        else
        {
            // Shorten if too long
            var shortened = draft.Length > 50
                ? draft[..47] + "..."
                : $"Quick update: {draft}";
            suggestions.Add(new SubjectSuggestion(
                shortened,
                "Shorter subject lines (under 50 characters) tend to have higher open rates on mobile."));
        }

        return new SubjectSuggestionResponse(suggestions);
    }

    public SendTimeRecommendation GetSendTimeRecommendation(int? segmentId)
    {
        return GetSendTimeRecommendationAsync(segmentId).GetAwaiter().GetResult();
    }

    public async Task<SendTimeRecommendation> GetSendTimeRecommendationAsync(int? segmentId)
    {
        // Analyze historical engagement patterns
        var query = _db.ActivityEvents.Where(e => e.EventType == "open");

        if (segmentId.HasValue)
        {
            var segmentContacts = await _segmentService.EvaluateSegmentAsync(segmentId.Value);
            var contactIds = segmentContacts.Select(c => c.Id).ToHashSet();
            query = query.Where(e => contactIds.Contains(e.ContactId));
        }

        var openEvents = await query
            .Select(e => new { e.OccurredAt })
            .ToListAsync();

        if (openEvents.Count < 5)
        {
            // Not enough data, return industry defaults
            return new SendTimeRecommendation(
                10,
                "Tuesday",
                "With limited engagement data, industry research suggests Tuesday at 10:00 AM has the highest average open rates across B2B campaigns.");
        }

        // Analyze by hour of day
        var hourGroups = openEvents
            .GroupBy(e => e.OccurredAt.Hour)
            .Select(g => new { Hour = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ToList();

        var bestHour = hourGroups.First().Hour;

        // Analyze by day of week
        var dayGroups = openEvents
            .GroupBy(e => e.OccurredAt.DayOfWeek)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ToList();

        var bestDay = dayGroups.First().Day;

        return new SendTimeRecommendation(
            bestHour,
            bestDay.ToString(),
            $"Based on {openEvents.Count} historical email opens, your audience is most engaged on {bestDay}s around {bestHour}:00. " +
            $"The top 3 hours are {string.Join(", ", hourGroups.Take(3).Select(h => $"{h.Hour}:00 ({h.Count} opens)"))}.");
    }
}
