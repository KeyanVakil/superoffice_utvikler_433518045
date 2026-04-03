using FluentAssertions;
using MarketFlow.Api.Models;
using MarketFlow.Api.Services;
using Xunit;

namespace MarketFlow.Tests.Unit;

public class SegmentServiceTests
{
    private SegmentService CreateService(string? dbName = null)
    {
        var db = TestDbContextFactory.Create(dbName);
        return new SegmentService(db);
    }

    private async Task SeedContacts(string dbName)
    {
        using var db = TestDbContextFactory.Create(dbName);

        db.Contacts.AddRange(
            new Contact { Id = 1, FirstName = "Alice", LastName = "Smith", Email = "alice@techcorp.com", Company = "TechCorp", Industry = "Technology" },
            new Contact { Id = 2, FirstName = "Bob", LastName = "Jones", Email = "bob@healthinc.com", Company = "HealthInc", Industry = "Healthcare" },
            new Contact { Id = 3, FirstName = "Charlie", LastName = "Brown", Email = "charlie@techstart.com", Company = "TechStart", Industry = "Technology" },
            new Contact { Id = 4, FirstName = "Diana", LastName = "Lee", Email = "diana@finbank.com", Company = "FinBank", Industry = "Finance" },
            new Contact { Id = 5, FirstName = "Eve", LastName = "Wilson", Email = "eve@techgiant.com", Company = "TechGiant", Industry = "Technology" }
        );
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Test_AND_Rules_In_Same_Group()
    {
        var dbName = nameof(Test_AND_Rules_In_Same_Group);
        await SeedContacts(dbName);

        using var db = TestDbContextFactory.Create(dbName);
        var service = new SegmentService(db);

        // Create segment: industry = Technology AND company contains "Tech"
        var segment = new Segment
        {
            Name = "Tech Companies",
            Rules = new List<SegmentRule>
            {
                new() { GroupIndex = 0, Field = "industry", Operator = "equals", Value = "Technology", SortOrder = 0 },
                new() { GroupIndex = 0, Field = "company", Operator = "contains", Value = "Tech", SortOrder = 1 }
            }
        };
        db.Segments.Add(segment);
        await db.SaveChangesAsync();

        var result = await service.EvaluateSegmentAsync(segment.Id);

        // Alice (TechCorp, Technology), Charlie (TechStart, Technology), Eve (TechGiant, Technology) match
        result.Should().HaveCount(3);
        result.Select(c => c.FirstName).Should().Contain(new[] { "Alice", "Charlie", "Eve" });
        result.Select(c => c.FirstName).Should().NotContain("Bob");
        result.Select(c => c.FirstName).Should().NotContain("Diana");
    }

    [Fact]
    public async Task Test_OR_Groups()
    {
        var dbName = nameof(Test_OR_Groups);
        await SeedContacts(dbName);

        using var db = TestDbContextFactory.Create(dbName);
        var service = new SegmentService(db);

        // Group 0: industry = Technology, Group 1: industry = Healthcare
        var segment = new Segment
        {
            Name = "Tech or Health",
            Rules = new List<SegmentRule>
            {
                new() { GroupIndex = 0, Field = "industry", Operator = "equals", Value = "Technology", SortOrder = 0 },
                new() { GroupIndex = 1, Field = "industry", Operator = "equals", Value = "Healthcare", SortOrder = 0 }
            }
        };
        db.Segments.Add(segment);
        await db.SaveChangesAsync();

        var result = await service.EvaluateSegmentAsync(segment.Id);

        // Alice, Charlie, Eve (Technology) + Bob (Healthcare) = 4
        result.Should().HaveCount(4);
        result.Select(c => c.FirstName).Should().Contain(new[] { "Alice", "Bob", "Charlie", "Eve" });
        result.Select(c => c.FirstName).Should().NotContain("Diana");
    }

    [Fact]
    public async Task Test_EngagementScore_Filter()
    {
        var dbName = nameof(Test_EngagementScore_Filter);

        using var db = TestDbContextFactory.Create(dbName);
        var contact1 = new Contact { FirstName = "Active", LastName = "User", Email = "active@test.com", Industry = "Tech" };
        var contact2 = new Contact { FirstName = "Passive", LastName = "User", Email = "passive@test.com", Industry = "Tech" };
        db.Contacts.AddRange(contact1, contact2);
        await db.SaveChangesAsync();

        // Give contact1 many recent events (high engagement)
        for (int i = 0; i < 10; i++)
        {
            db.ActivityEvents.Add(new ActivityEvent
            {
                ContactId = contact1.Id,
                EventType = "open",
                OccurredAt = DateTime.UtcNow.AddDays(-i)
            });
        }
        // Give contact2 only 1 event
        db.ActivityEvents.Add(new ActivityEvent
        {
            ContactId = contact2.Id,
            EventType = "open",
            OccurredAt = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();

        var segment = new Segment
        {
            Name = "High Engagement",
            Rules = new List<SegmentRule>
            {
                new() { GroupIndex = 0, Field = "engagementScore", Operator = "greaterThan", Value = "5", SortOrder = 0 }
            }
        };
        db.Segments.Add(segment);
        await db.SaveChangesAsync();

        var service = new SegmentService(db);
        var result = await service.EvaluateSegmentAsync(segment.Id);

        // contact1 has 10 opens * 2 = 20 score (> 5), contact2 has 1 open * 2 = 2 (< 5)
        result.Should().HaveCount(1);
        result.First().FirstName.Should().Be("Active");
    }

    [Fact]
    public async Task Test_Tag_Filter()
    {
        var dbName = nameof(Test_Tag_Filter);

        using var db = TestDbContextFactory.Create(dbName);
        var contact1 = new Contact { FirstName = "Tagged", LastName = "One", Email = "tagged@test.com" };
        var contact2 = new Contact { FirstName = "Untagged", LastName = "Two", Email = "untagged@test.com" };
        db.Contacts.AddRange(contact1, contact2);
        await db.SaveChangesAsync();

        var tag = new Tag { Name = "vip" };
        db.Tags.Add(tag);
        await db.SaveChangesAsync();

        db.ContactTags.Add(new ContactTag { ContactId = contact1.Id, TagId = tag.Id });
        await db.SaveChangesAsync();

        var segment = new Segment
        {
            Name = "VIP Contacts",
            Rules = new List<SegmentRule>
            {
                new() { GroupIndex = 0, Field = "tag", Operator = "equals", Value = "vip", SortOrder = 0 }
            }
        };
        db.Segments.Add(segment);
        await db.SaveChangesAsync();

        var service = new SegmentService(db);
        var result = await service.EvaluateSegmentAsync(segment.Id);

        result.Should().HaveCount(1);
        result.First().FirstName.Should().Be("Tagged");
    }

    [Fact]
    public async Task Test_Empty_Segment_Returns_No_Contacts()
    {
        var dbName = nameof(Test_Empty_Segment_Returns_No_Contacts);
        await SeedContacts(dbName);

        using var db = TestDbContextFactory.Create(dbName);
        var service = new SegmentService(db);

        // Rule that matches nobody
        var segment = new Segment
        {
            Name = "Nobody",
            Rules = new List<SegmentRule>
            {
                new() { GroupIndex = 0, Field = "industry", Operator = "equals", Value = "Aerospace", SortOrder = 0 }
            }
        };
        db.Segments.Add(segment);
        await db.SaveChangesAsync();

        var result = await service.EvaluateSegmentAsync(segment.Id);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Test_Preview_Returns_Count_And_Sample()
    {
        var dbName = nameof(Test_Preview_Returns_Count_And_Sample);

        using var db = TestDbContextFactory.Create(dbName);

        // Create 15 contacts in Technology
        for (int i = 1; i <= 15; i++)
        {
            db.Contacts.Add(new Contact
            {
                FirstName = $"User{i}",
                LastName = "Test",
                Email = $"user{i}@test.com",
                Industry = "Technology"
            });
        }
        await db.SaveChangesAsync();

        var segment = new Segment
        {
            Name = "All Tech",
            Rules = new List<SegmentRule>
            {
                new() { GroupIndex = 0, Field = "industry", Operator = "equals", Value = "Technology", SortOrder = 0 }
            }
        };
        db.Segments.Add(segment);
        await db.SaveChangesAsync();

        var service = new SegmentService(db);
        var preview = await service.PreviewSegmentAsync(segment.Id);

        preview.MatchingCount.Should().Be(15);
        preview.SampleContacts.Should().HaveCountLessOrEqualTo(10);
        preview.SegmentId.Should().Be(segment.Id);
    }
}
