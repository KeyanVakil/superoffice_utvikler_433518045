namespace MarketFlow.Api.Dtos;

public record CampaignListDto(
    int Id,
    string Name,
    string Subject,
    string Status,
    string? SegmentName,
    DateTime? SentAt,
    int SendCount,
    double OpenRate,
    double ClickRate
);

public record CampaignDetailDto(
    int Id,
    string Name,
    string Subject,
    string HtmlBody,
    string Status,
    int? SegmentId,
    string? SegmentName,
    DateTime? SentAt,
    DateTime CreatedAt,
    int SendCount,
    double OpenRate,
    double ClickRate
);

public record CreateCampaignDto(
    string Name,
    string Subject,
    string HtmlBody,
    int? SegmentId
);

public record CampaignStatsDto(
    int CampaignId,
    int TotalSent,
    int TotalOpens,
    int TotalClicks,
    double OpenRate,
    double ClickThroughRate,
    List<DayStats> Timeline
);

public record DayStats(
    DateOnly Date,
    int Sends,
    int Opens,
    int Clicks
);
