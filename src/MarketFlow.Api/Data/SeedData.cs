using MarketFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MarketFlow.Api.Data;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Tags.AnyAsync())
            return;

        var rng = new Random(42);

        // Tags
        var tagNames = new[] { "prospect", "customer", "enterprise", "smb", "newsletter", "active", "vip", "churned", "trial", "partner" };
        var tags = tagNames.Select(n => new Tag { Name = n }).ToList();
        db.Tags.AddRange(tags);
        await db.SaveChangesAsync();

        // Contacts
        var industries = new[] { "Technology", "Healthcare", "Finance", "Energy", "Education", "Retail", "Manufacturing" };
        var firstNames = new[] { "Emma", "Liam", "Olivia", "Noah", "Ava", "Ethan", "Sophia", "Mason", "Isabella", "Logan",
            "Mia", "Lucas", "Charlotte", "Alexander", "Amelia", "Jacob", "Harper", "Michael", "Evelyn", "Daniel",
            "Abigail", "Henry", "Emily", "Sebastian", "Elizabeth", "Jack", "Sofia", "Aiden", "Avery", "Owen",
            "Ella", "Samuel", "Scarlett", "Ryan", "Grace", "Nathan", "Lily", "Caleb", "Chloe", "Christian",
            "Victoria", "Dylan", "Riley", "Isaac", "Aria", "Luke", "Zoey", "Gabriel", "Penelope", "Anthony" };
        var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez",
            "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin",
            "Lee", "Perez", "Thompson", "White", "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson",
            "Walker", "Young", "Allen", "King", "Wright", "Scott", "Torres", "Nguyen", "Hill", "Flores",
            "Green", "Adams", "Nelson", "Baker", "Hall", "Rivera", "Campbell", "Mitchell", "Carter", "Roberts" };
        var companies = new[] { "TechCorp", "MediStar", "FinanceHub", "EnergyPlus", "EduTech", "RetailMax", "ManuPro",
            "CloudNine", "DataFlow", "NetSphere", "BioGen", "GreenWave", "PayScale", "LearnIT", "ShopSmart",
            "BuildRight", "CodeWorks", "HealthLink", "TradePeak", "SolarEdge" };

        var contacts = new List<Contact>();
        for (int i = 0; i < 100; i++)
        {
            var firstName = firstNames[i % firstNames.Length];
            var lastName = lastNames[i % lastNames.Length];
            var company = companies[rng.Next(companies.Length)];
            var industry = industries[rng.Next(industries.Length)];
            var createdAt = DateTime.UtcNow.AddDays(-rng.Next(30, 365));

            contacts.Add(new Contact
            {
                FirstName = firstName,
                LastName = lastName,
                Email = $"{firstName.ToLower()}.{lastName.ToLower()}{i}@{company.ToLower()}.com",
                Company = company,
                Industry = industry,
                CreatedAt = createdAt,
                UpdatedAt = createdAt.AddDays(rng.Next(0, 30))
            });
        }
        db.Contacts.AddRange(contacts);
        await db.SaveChangesAsync();

        // ContactTags
        var contactTags = new List<ContactTag>();
        foreach (var contact in contacts)
        {
            int tagCount = rng.Next(1, 4);
            var selectedTags = tags.OrderBy(_ => rng.Next()).Take(tagCount).ToList();
            foreach (var tag in selectedTags)
            {
                contactTags.Add(new ContactTag { ContactId = contact.Id, TagId = tag.Id });
            }
        }
        db.ContactTags.AddRange(contactTags);
        await db.SaveChangesAsync();

        // Segments
        var segment1 = new Segment
        {
            Name = "Tech Enterprise Customers",
            Description = "Enterprise customers in the technology industry",
            CreatedAt = DateTime.UtcNow.AddDays(-60)
        };
        var segment2 = new Segment
        {
            Name = "Active Newsletter Subscribers",
            Description = "Contacts subscribed to the newsletter who have been active recently",
            CreatedAt = DateTime.UtcNow.AddDays(-45)
        };
        var segment3 = new Segment
        {
            Name = "Healthcare Prospects",
            Description = "Prospects in the healthcare industry",
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };
        db.Segments.AddRange(segment1, segment2, segment3);
        await db.SaveChangesAsync();

        // Segment rules
        db.SegmentRules.AddRange(
            new SegmentRule { SegmentId = segment1.Id, GroupIndex = 0, Field = "Industry", Operator = "equals", Value = "Technology", SortOrder = 0 },
            new SegmentRule { SegmentId = segment1.Id, GroupIndex = 0, Field = "Company", Operator = "contains", Value = "Tech", SortOrder = 1 },
            new SegmentRule { SegmentId = segment2.Id, GroupIndex = 0, Field = "Industry", Operator = "equals", Value = "Technology", SortOrder = 0 },
            new SegmentRule { SegmentId = segment2.Id, GroupIndex = 1, Field = "Industry", Operator = "equals", Value = "Education", SortOrder = 0 },
            new SegmentRule { SegmentId = segment3.Id, GroupIndex = 0, Field = "Industry", Operator = "equals", Value = "Healthcare", SortOrder = 0 }
        );
        await db.SaveChangesAsync();

        // Campaigns
        var sentCampaign = new Campaign
        {
            Name = "Spring Product Launch",
            Subject = "Introducing Our Latest Features for {{firstName}}",
            HtmlBody = "<html><body><h1>Hello {{firstName}}!</h1><p>We're excited to announce our spring product launch at {{company}}.</p><p><a href='https://example.com/launch'>Learn More</a></p></body></html>",
            SegmentId = segment1.Id,
            Status = "Sent",
            SentAt = DateTime.UtcNow.AddDays(-14),
            CreatedAt = DateTime.UtcNow.AddDays(-21)
        };
        var draftCampaign = new Campaign
        {
            Name = "Summer Newsletter",
            Subject = "What's New This Summer at MarketFlow",
            HtmlBody = "<html><body><h1>Summer Updates</h1><p>Check out what we've been working on.</p></body></html>",
            SegmentId = segment2.Id,
            Status = "Draft",
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };
        db.Campaigns.AddRange(sentCampaign, draftCampaign);
        await db.SaveChangesAsync();

        // Activity events for sent campaign (realistic rates: ~100% send, ~35% open, ~12% click)
        var sentCampaignContacts = contacts.Take(60).ToList();
        var events = new List<ActivityEvent>();
        var sentAt = sentCampaign.SentAt!.Value;

        foreach (var contact in sentCampaignContacts)
        {
            events.Add(new ActivityEvent
            {
                ContactId = contact.Id,
                CampaignId = sentCampaign.Id,
                EventType = "send",
                OccurredAt = sentAt.AddMinutes(rng.Next(0, 60))
            });

            if (rng.NextDouble() < 0.35)
            {
                var openTime = sentAt.AddHours(rng.Next(1, 72));
                events.Add(new ActivityEvent
                {
                    ContactId = contact.Id,
                    CampaignId = sentCampaign.Id,
                    EventType = "open",
                    OccurredAt = openTime
                });

                if (rng.NextDouble() < 0.35)
                {
                    events.Add(new ActivityEvent
                    {
                        ContactId = contact.Id,
                        CampaignId = sentCampaign.Id,
                        EventType = "click",
                        OccurredAt = openTime.AddMinutes(rng.Next(1, 30)),
                        Metadata = "{\"url\":\"https://example.com/launch\"}"
                    });
                }
            }
        }
        db.ActivityEvents.AddRange(events);
        await db.SaveChangesAsync();

        // Journey
        var journey = new Journey
        {
            Name = "New Customer Onboarding",
            TriggerType = "tag_added",
            TriggerConfig = "{\"tag\":\"customer\"}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-90)
        };
        db.Journeys.Add(journey);
        await db.SaveChangesAsync();

        var step1 = new JourneyStep
        {
            JourneyId = journey.Id,
            StepOrder = 1,
            StepType = "send_email",
            Config = "{\"subject\":\"Welcome aboard!\",\"template\":\"welcome_email\"}"
        };
        var step2 = new JourneyStep
        {
            JourneyId = journey.Id,
            StepOrder = 2,
            StepType = "wait",
            Config = "{\"days\":3}"
        };
        var step3 = new JourneyStep
        {
            JourneyId = journey.Id,
            StepOrder = 3,
            StepType = "condition",
            Config = "{\"field\":\"opened_email\",\"operator\":\"equals\",\"value\":\"true\"}"
        };
        var step4 = new JourneyStep
        {
            JourneyId = journey.Id,
            StepOrder = 4,
            StepType = "send_email",
            Config = "{\"subject\":\"Getting started guide\",\"template\":\"getting_started\"}"
        };
        var step5 = new JourneyStep
        {
            JourneyId = journey.Id,
            StepOrder = 5,
            StepType = "send_email",
            Config = "{\"subject\":\"We miss you!\",\"template\":\"re_engagement\"}"
        };
        db.JourneySteps.AddRange(step1, step2, step3, step4, step5);
        await db.SaveChangesAsync();

        // Wire up next step pointers
        step1.TrueNextStepId = step2.Id;
        step2.TrueNextStepId = step3.Id;
        step3.TrueNextStepId = step4.Id;
        step3.FalseNextStepId = step5.Id;
        await db.SaveChangesAsync();

        // Enrollments
        var enrolledContacts = contacts.Where(c => contactTags.Any(ct => ct.ContactId == c.Id && ct.TagId == tags.First(t => t.Name == "customer").Id)).Take(15).ToList();
        foreach (var contact in enrolledContacts)
        {
            var enrolledAt = DateTime.UtcNow.AddDays(-rng.Next(5, 60));
            var completed = rng.NextDouble() < 0.4;
            var currentStep = completed ? null : (int?)new[] { step1.Id, step2.Id, step3.Id, step4.Id, step5.Id }[rng.Next(5)];

            var enrollment = new JourneyEnrollment
            {
                JourneyId = journey.Id,
                ContactId = contact.Id,
                CurrentStepId = currentStep,
                Status = completed ? "Completed" : "Active",
                EnrolledAt = enrolledAt,
                CompletedAt = completed ? enrolledAt.AddDays(rng.Next(7, 20)) : null
            };
            db.JourneyEnrollments.Add(enrollment);
        }
        await db.SaveChangesAsync();
    }
}
