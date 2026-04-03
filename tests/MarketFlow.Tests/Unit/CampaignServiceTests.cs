using FluentAssertions;
using MarketFlow.Api.Models;
using MarketFlow.Api.Services;
using Xunit;

namespace MarketFlow.Tests.Unit;

public class CampaignServiceTests
{
    [Fact]
    public void Test_Personalize_Content_Replaces_Tokens()
    {
        var contact = new Contact
        {
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@example.com",
            Company = "TechCorp"
        };

        var template = "Hello {{firstName}} {{lastName}}, welcome from {{company}}! Contact: {{email}}";
        var result = CampaignService.PersonalizeContent(template, contact);

        result.Should().Be("Hello Alice Smith, welcome from TechCorp! Contact: alice@example.com");
    }

    [Fact]
    public void Test_Personalize_Content_Handles_Missing_Values()
    {
        var contact = new Contact
        {
            FirstName = "Bob",
            LastName = "Jones",
            Email = "bob@test.com",
            Company = null // Missing company
        };

        var template = "Hi {{firstName}}, your company is {{company}}.";
        var result = CampaignService.PersonalizeContent(template, contact);

        result.Should().Be("Hi Bob, your company is .");
    }

    [Fact]
    public async Task Test_Send_Campaign_Creates_Send_Events()
    {
        var dbName = nameof(Test_Send_Campaign_Creates_Send_Events);
        using var db = TestDbContextFactory.Create(dbName);

        // Create contacts
        var contact1 = new Contact { FirstName = "Alice", LastName = "A", Email = "alice@test.com", Industry = "Technology" };
        var contact2 = new Contact { FirstName = "Bob", LastName = "B", Email = "bob@test.com", Industry = "Technology" };
        var contact3 = new Contact { FirstName = "Charlie", LastName = "C", Email = "charlie@test.com", Industry = "Finance" };
        db.Contacts.AddRange(contact1, contact2, contact3);
        await db.SaveChangesAsync();

        // Create segment matching Technology contacts
        var segment = new Segment
        {
            Name = "Tech Segment",
            Rules = new List<SegmentRule>
            {
                new() { GroupIndex = 0, Field = "industry", Operator = "equals", Value = "Technology", SortOrder = 0 }
            }
        };
        db.Segments.Add(segment);
        await db.SaveChangesAsync();

        // Create campaign
        var campaign = new Campaign
        {
            Name = "Test Campaign",
            Subject = "Hello {{firstName}}",
            HtmlBody = "<p>Welcome {{firstName}}!</p>",
            SegmentId = segment.Id,
            Status = "Draft"
        };
        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync();

        var service = new CampaignService(db, new SegmentService(db));
        await service.SendCampaignAsync(campaign.Id);

        // Verify send events created for Alice and Bob (Technology), not Charlie (Finance)
        var events = db.ActivityEvents.Where(e => e.CampaignId == campaign.Id).ToList();
        events.Should().HaveCount(2);
        events.Should().AllSatisfy(e => e.EventType.Should().Be("send"));
        events.Select(e => e.ContactId).Should().Contain(new[] { contact1.Id, contact2.Id });
        events.Select(e => e.ContactId).Should().NotContain(contact3.Id);

        // Verify campaign status updated
        var updatedCampaign = db.Campaigns.Find(campaign.Id)!;
        updatedCampaign.Status.Should().Be("Sent");
        updatedCampaign.SentAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Test_Send_Already_Sent_Campaign_Throws()
    {
        var dbName = nameof(Test_Send_Already_Sent_Campaign_Throws);
        using var db = TestDbContextFactory.Create(dbName);

        var campaign = new Campaign
        {
            Name = "Already Sent",
            Subject = "Old",
            HtmlBody = "<p>Old</p>",
            Status = "Sent",
            SentAt = DateTime.UtcNow.AddDays(-1)
        };
        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync();

        var service = new CampaignService(db, new SegmentService(db));

        var act = () => service.SendCampaignAsync(campaign.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already*sent*");
    }

    [Fact]
    public async Task Test_Campaign_Stats_Calculates_Rates()
    {
        var dbName = nameof(Test_Campaign_Stats_Calculates_Rates);
        using var db = TestDbContextFactory.Create(dbName);

        var contact1 = new Contact { FirstName = "A", LastName = "A", Email = "a@test.com" };
        var contact2 = new Contact { FirstName = "B", LastName = "B", Email = "b@test.com" };
        var contact3 = new Contact { FirstName = "C", LastName = "C", Email = "c@test.com" };
        var contact4 = new Contact { FirstName = "D", LastName = "D", Email = "d@test.com" };
        db.Contacts.AddRange(contact1, contact2, contact3, contact4);
        await db.SaveChangesAsync();

        var campaign = new Campaign { Name = "Stats Test", Subject = "Test", HtmlBody = "<p>Test</p>", Status = "Sent", SentAt = DateTime.UtcNow };
        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync();

        var now = DateTime.UtcNow;
        // 4 sends, 2 opens, 1 click
        db.ActivityEvents.AddRange(
            new ActivityEvent { ContactId = contact1.Id, CampaignId = campaign.Id, EventType = "send", OccurredAt = now },
            new ActivityEvent { ContactId = contact2.Id, CampaignId = campaign.Id, EventType = "send", OccurredAt = now },
            new ActivityEvent { ContactId = contact3.Id, CampaignId = campaign.Id, EventType = "send", OccurredAt = now },
            new ActivityEvent { ContactId = contact4.Id, CampaignId = campaign.Id, EventType = "send", OccurredAt = now },
            new ActivityEvent { ContactId = contact1.Id, CampaignId = campaign.Id, EventType = "open", OccurredAt = now.AddMinutes(5) },
            new ActivityEvent { ContactId = contact2.Id, CampaignId = campaign.Id, EventType = "open", OccurredAt = now.AddMinutes(10) },
            new ActivityEvent { ContactId = contact1.Id, CampaignId = campaign.Id, EventType = "click", OccurredAt = now.AddMinutes(15) }
        );
        await db.SaveChangesAsync();

        var service = new CampaignService(db, new SegmentService(db));
        var stats = await service.GetCampaignStatsAsync(campaign.Id);

        stats.TotalSent.Should().Be(4);
        stats.TotalOpens.Should().Be(2);
        stats.TotalClicks.Should().Be(1);
        stats.OpenRate.Should().BeApproximately(0.5, 0.01);        // 2/4
        stats.ClickThroughRate.Should().BeApproximately(0.25, 0.01); // 1/4
    }
}
