using Microsoft.EntityFrameworkCore;
using MarketFlow.Api.Data;
using MarketFlow.Api.Dtos;
using MarketFlow.Api.Models;

namespace MarketFlow.Api.Services;

public class CampaignService
{
    private readonly AppDbContext _db;
    private readonly SegmentService _segmentService;

    public CampaignService(AppDbContext db, SegmentService segmentService)
    {
        _db = db;
        _segmentService = segmentService;
    }

    public async Task<List<CampaignListDto>> GetCampaignsAsync()
    {
        var campaigns = await _db.Campaigns
            .Include(c => c.Segment)
            .Include(c => c.ActivityEvents)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return campaigns.Select(MapToListDto).ToList();
    }

    public async Task<CampaignDetailDto?> GetCampaignAsync(int id)
    {
        var campaign = await _db.Campaigns
            .Include(c => c.Segment)
            .Include(c => c.ActivityEvents)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (campaign is null) return null;

        var sends = campaign.ActivityEvents.Count(e => e.EventType == "send");
        var opens = campaign.ActivityEvents.Count(e => e.EventType == "open");
        var clicks = campaign.ActivityEvents.Count(e => e.EventType == "click");

        return new CampaignDetailDto(
            campaign.Id, campaign.Name, campaign.Subject, campaign.HtmlBody,
            campaign.Status, campaign.SegmentId, campaign.Segment?.Name,
            campaign.SentAt, campaign.CreatedAt,
            sends,
            sends > 0 ? Math.Round((double)opens / sends, 4) : 0,
            sends > 0 ? Math.Round((double)clicks / sends, 4) : 0);
    }

    public async Task<CampaignDetailDto> CreateCampaignAsync(CreateCampaignDto dto)
    {
        var campaign = new Campaign
        {
            Name = dto.Name,
            Subject = dto.Subject,
            HtmlBody = dto.HtmlBody,
            SegmentId = dto.SegmentId,
            Status = "Draft",
            CreatedAt = DateTime.UtcNow
        };
        _db.Campaigns.Add(campaign);
        await _db.SaveChangesAsync();

        return (await GetCampaignAsync(campaign.Id))!;
    }

    public async Task<CampaignDetailDto?> UpdateCampaignAsync(int id, CreateCampaignDto dto)
    {
        var campaign = await _db.Campaigns.FindAsync(id);
        if (campaign is null) return null;

        if (campaign.Status == "Sent")
            throw new InvalidOperationException("Cannot update a sent campaign.");

        campaign.Name = dto.Name;
        campaign.Subject = dto.Subject;
        campaign.HtmlBody = dto.HtmlBody;
        campaign.SegmentId = dto.SegmentId;
        await _db.SaveChangesAsync();

        return (await GetCampaignAsync(id))!;
    }

    public async Task<bool> DeleteCampaignAsync(int id)
    {
        var campaign = await _db.Campaigns.FindAsync(id);
        if (campaign is null) return false;

        _db.Campaigns.Remove(campaign);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<CampaignDetailDto?> SendCampaignAsync(int id)
    {
        var campaign = await _db.Campaigns.FindAsync(id);
        if (campaign is null) return null;

        if (campaign.Status == "Sent")
            throw new InvalidOperationException("Campaign has already been sent.");

        if (campaign.SegmentId is null)
            throw new InvalidOperationException("Campaign must have a segment assigned before sending.");

        var contacts = await _segmentService.EvaluateSegmentAsync(campaign.SegmentId.Value);

        var now = DateTime.UtcNow;
        var events = contacts.Select(c => new ActivityEvent
        {
            ContactId = c.Id,
            CampaignId = campaign.Id,
            EventType = "send",
            OccurredAt = now
        }).ToList();

        _db.ActivityEvents.AddRange(events);
        campaign.Status = "Sent";
        campaign.SentAt = now;
        await _db.SaveChangesAsync();

        return (await GetCampaignAsync(id))!;
    }

    public async Task<CampaignStatsDto?> GetCampaignStatsAsync(int id)
    {
        var campaign = await _db.Campaigns
            .Include(c => c.ActivityEvents)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (campaign is null) return null;

        var events = campaign.ActivityEvents;
        int totalSent = events.Count(e => e.EventType == "send");
        int totalOpens = events.Count(e => e.EventType == "open");
        int totalClicks = events.Count(e => e.EventType == "click");

        var timeline = events
            .GroupBy(e => DateOnly.FromDateTime(e.OccurredAt))
            .OrderBy(g => g.Key)
            .Select(g => new DayStats(
                g.Key,
                g.Count(e => e.EventType == "send"),
                g.Count(e => e.EventType == "open"),
                g.Count(e => e.EventType == "click")))
            .ToList();

        return new CampaignStatsDto(
            id, totalSent, totalOpens, totalClicks,
            totalSent > 0 ? Math.Round((double)totalOpens / totalSent, 4) : 0,
            totalSent > 0 ? Math.Round((double)totalClicks / totalSent, 4) : 0,
            timeline);
    }

    public static string PersonalizeContent(string template, Contact contact)
    {
        return template
            .Replace("{{firstName}}", contact.FirstName)
            .Replace("{{lastName}}", contact.LastName)
            .Replace("{{company}}", contact.Company ?? string.Empty)
            .Replace("{{email}}", contact.Email);
    }

    private static CampaignListDto MapToListDto(Campaign c)
    {
        var sends = c.ActivityEvents.Count(e => e.EventType == "send");
        var opens = c.ActivityEvents.Count(e => e.EventType == "open");
        var clicks = c.ActivityEvents.Count(e => e.EventType == "click");

        return new CampaignListDto(
            c.Id, c.Name, c.Subject, c.Status, c.Segment?.Name, c.SentAt,
            sends,
            sends > 0 ? Math.Round((double)opens / sends, 4) : 0,
            sends > 0 ? Math.Round((double)clicks / sends, 4) : 0);
    }
}
