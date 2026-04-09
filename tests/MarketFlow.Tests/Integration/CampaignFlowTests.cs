using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MarketFlow.Api.Data;
using MarketFlow.Api.Dtos;
using MarketFlow.Api.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MarketFlow.Tests.Integration;

public class CampaignFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public CampaignFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Test_Full_Campaign_Flow()
    {
        // 1. Create contacts with a unique industry to avoid collisions with seed data
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var uniqueIndustry = $"FlowTest_{uniqueId}";
        var contact1 = new CreateContactDto("Flow1", "User", $"flow1-{uniqueId}@test.com", "FlowCorp", uniqueIndustry, null);
        var contact2 = new CreateContactDto("Flow2", "User", $"flow2-{uniqueId}@test.com", "FlowCorp", uniqueIndustry, null);
        var contact3 = new CreateContactDto("Flow3", "User", $"flow3-{uniqueId}@test.com", "OtherCorp", "Finance", null);

        var r1 = await _client.PostAsJsonAsync("/api/contacts", contact1);
        var r2 = await _client.PostAsJsonAsync("/api/contacts", contact2);
        var r3 = await _client.PostAsJsonAsync("/api/contacts", contact3);
        r1.StatusCode.Should().Be(HttpStatusCode.Created);
        r2.StatusCode.Should().Be(HttpStatusCode.Created);
        r3.StatusCode.Should().Be(HttpStatusCode.Created);

        // 2. Create segment targeting the unique industry
        var segmentDto = new CreateSegmentDto("Flow Tech Segment", "Test segment", new List<SegmentRuleDto>
        {
            new(0, "industry", "equals", uniqueIndustry)
        });
        var segmentResponse = await _client.PostAsJsonAsync("/api/segments", segmentDto);
        segmentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var segment = await segmentResponse.Content.ReadFromJsonAsync<SegmentDetailDto>();

        // 3. Create campaign
        var campaignDto = new CreateCampaignDto(
            "Flow Campaign", "Hello {{firstName}}!", "<h1>Welcome {{firstName}}</h1>", segment!.Id);
        var campaignResponse = await _client.PostAsJsonAsync("/api/campaigns", campaignDto);
        campaignResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var campaign = await campaignResponse.Content.ReadFromJsonAsync<CampaignDetailDto>();
        campaign!.Status.Should().Be("Draft");

        // 4. Send campaign
        var sendResponse = await _client.PostAsync($"/api/campaigns/{campaign.Id}/send", null);
        sendResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Verify campaign is now Sent
        var getCampaignResponse = await _client.GetAsync($"/api/campaigns/{campaign.Id}");
        var sentCampaign = await getCampaignResponse.Content.ReadFromJsonAsync<CampaignDetailDto>();
        sentCampaign!.Status.Should().Be("Sent");
        sentCampaign.SentAt.Should().NotBeNull();
        sentCampaign.SendCount.Should().Be(2); // Only contacts with the unique industry

        // 6. Verify stats
        var statsResponse = await _client.GetAsync($"/api/campaigns/{campaign.Id}/stats");
        statsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await statsResponse.Content.ReadFromJsonAsync<CampaignStatsDto>();
        stats!.TotalSent.Should().Be(2);
        stats.OpenRate.Should().Be(0); // No opens yet
    }

    [Fact]
    public async Task Test_Campaign_Stats_Timeline()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        // Create contacts
        var c1 = new CreateContactDto("Timeline1", "User", $"tl1-{uniqueId}@test.com", "Co", "Technology", null);
        var c2 = new CreateContactDto("Timeline2", "User", $"tl2-{uniqueId}@test.com", "Co", "Technology", null);
        await _client.PostAsJsonAsync("/api/contacts", c1);
        await _client.PostAsJsonAsync("/api/contacts", c2);

        // Create segment
        var segDto = new CreateSegmentDto("Timeline Segment", null, new List<SegmentRuleDto>
        {
            new(0, "industry", "equals", "Technology")
        });
        var segResp = await _client.PostAsJsonAsync("/api/segments", segDto);
        var seg = await segResp.Content.ReadFromJsonAsync<SegmentDetailDto>();

        // Create and send campaign
        var campDto = new CreateCampaignDto("Timeline Campaign", "Test", "<p>Test</p>", seg!.Id);
        var campResp = await _client.PostAsJsonAsync("/api/campaigns", campDto);
        var camp = await campResp.Content.ReadFromJsonAsync<CampaignDetailDto>();

        await _client.PostAsync($"/api/campaigns/{camp!.Id}/send", null);

        // Simulate opens and clicks via the activity events endpoint
        // (We need to directly insert events since they'd normally come from tracking pixels)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var contacts = db.Contacts.Where(c => c.Email.Contains(uniqueId)).ToList();
        var now = DateTime.UtcNow;

        foreach (var contact in contacts)
        {
            db.ActivityEvents.Add(new ActivityEvent
            {
                ContactId = contact.Id,
                CampaignId = camp.Id,
                EventType = "open",
                OccurredAt = now.AddMinutes(10)
            });
        }
        db.ActivityEvents.Add(new ActivityEvent
        {
            ContactId = contacts.First().Id,
            CampaignId = camp.Id,
            EventType = "click",
            OccurredAt = now.AddMinutes(20)
        });
        await db.SaveChangesAsync();

        // Verify stats reflect the opens and clicks
        var statsResponse = await _client.GetAsync($"/api/campaigns/{camp.Id}/stats");
        var stats = await statsResponse.Content.ReadFromJsonAsync<CampaignStatsDto>();

        stats!.TotalSent.Should().BeGreaterThanOrEqualTo(2);
        stats.TotalOpens.Should().Be(2);
        stats.TotalClicks.Should().Be(1);
        stats.OpenRate.Should().BeGreaterThan(0);
        stats.ClickThroughRate.Should().BeGreaterThan(0);
        stats.Timeline.Should().NotBeEmpty();
    }
}
