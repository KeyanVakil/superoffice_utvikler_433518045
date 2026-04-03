namespace MarketFlow.Api.Dtos;

public record OverviewDto(
    int TotalContacts,
    int ActiveCampaigns,
    int ActiveJourneys,
    double OverallEngagementRate,
    List<CampaignListDto> RecentCampaigns
);

public record EngagementTrendDto(
    DateOnly Date,
    int Sends,
    int Opens,
    int Clicks
);
