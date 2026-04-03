namespace MarketFlow.Api.Dtos;

public record SubjectSuggestionRequest(
    string DraftSubject,
    string? CampaignContext
);

public record SubjectSuggestionResponse(
    List<SubjectSuggestion> Suggestions
);

public record SubjectSuggestion(
    string Subject,
    string Reason
);

public record SendTimeRecommendation(
    int RecommendedHour,
    string RecommendedDay,
    string Reason
);
