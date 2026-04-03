using Microsoft.EntityFrameworkCore;
using MarketFlow.Api.Data;
using MarketFlow.Api.Dtos;

namespace MarketFlow.Api.Services;

public class AnalyticsService
{
    private readonly AppDbContext _db;

    public AnalyticsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<OverviewDto> GetOverviewAsync(int days = 30)
    {
        var totalContacts = await _db.Contacts.CountAsync();
        var activeCampaigns = await _db.Campaigns.CountAsync(c => c.Status == "Sent");
        var activeJourneys = await _db.Journeys.CountAsync(j => j.IsActive);

        var cutoff = DateTime.UtcNow.AddDays(-days);
        var recentEvents = await _db.ActivityEvents
            .Where(e => e.OccurredAt >= cutoff)
            .ToListAsync();

        var totalSends = recentEvents.Count(e => e.EventType == "send");
        var totalOpens = recentEvents.Count(e => e.EventType == "open");
        var engagementRate = totalSends > 0 ? Math.Round((double)totalOpens / totalSends * 100, 2) : 0;

        var recentCampaigns = await _db.Campaigns
            .Include(c => c.Segment)
            .Include(c => c.ActivityEvents)
            .OrderByDescending(c => c.CreatedAt)
            .Take(5)
            .ToListAsync();

        var campaignDtos = recentCampaigns.Select(c =>
        {
            var sends = c.ActivityEvents.Count(e => e.EventType == "send");
            var opens = c.ActivityEvents.Count(e => e.EventType == "open");
            var clicks = c.ActivityEvents.Count(e => e.EventType == "click");
            return new CampaignListDto(
                c.Id, c.Name, c.Subject, c.Status, c.Segment?.Name, c.SentAt,
                sends,
                sends > 0 ? Math.Round((double)opens / sends, 4) : 0,
                sends > 0 ? Math.Round((double)clicks / sends, 4) : 0);
        }).ToList();

        return new OverviewDto(totalContacts, activeCampaigns, activeJourneys, engagementRate, campaignDtos);
    }

    public async Task<List<EngagementTrendDto>> GetEngagementTrendsAsync(int days = 30)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);

        var events = await _db.ActivityEvents
            .Where(e => e.OccurredAt >= cutoff)
            .ToListAsync();

        var trends = Enumerable.Range(0, days)
            .Select(i => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-days + i + 1)))
            .Select(date =>
            {
                var dayEvents = events.Where(e => DateOnly.FromDateTime(e.OccurredAt) == date).ToList();
                return new EngagementTrendDto(
                    date,
                    dayEvents.Count(e => e.EventType == "send"),
                    dayEvents.Count(e => e.EventType == "open"),
                    dayEvents.Count(e => e.EventType == "click"));
            })
            .ToList();

        return trends;
    }
}
