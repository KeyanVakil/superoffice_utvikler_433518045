using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MarketFlow.Api.Dtos;
using Xunit;

namespace MarketFlow.Tests.Integration;

public class SegmentEvaluationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SegmentEvaluationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Test_Segment_Preview_Returns_Matching_Contacts()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        // Create contacts in different industries
        var techContacts = new[]
        {
            new CreateContactDto("TechA", "User", $"techa-{uniqueId}@test.com", "Corp1", "Technology", null),
            new CreateContactDto("TechB", "User", $"techb-{uniqueId}@test.com", "Corp2", "Technology", null),
            new CreateContactDto("TechC", "User", $"techc-{uniqueId}@test.com", "Corp3", "Technology", null),
        };
        var financeContact = new CreateContactDto("FinA", "User", $"fina-{uniqueId}@test.com", "Bank1", "Finance", null);

        foreach (var dto in techContacts)
        {
            var resp = await _client.PostAsJsonAsync("/api/contacts", dto);
            resp.StatusCode.Should().Be(HttpStatusCode.Created);
        }
        var finResp = await _client.PostAsJsonAsync("/api/contacts", financeContact);
        finResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Create segment for Technology
        var segmentDto = new CreateSegmentDto("Preview Test Segment", "Test", new List<SegmentRuleDto>
        {
            new(0, "industry", "equals", "Technology")
        });
        var segResponse = await _client.PostAsJsonAsync("/api/segments", segmentDto);
        segResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var segment = await segResponse.Content.ReadFromJsonAsync<SegmentDetailDto>();

        // Preview
        var previewResponse = await _client.GetAsync($"/api/segments/{segment!.Id}/preview");
        previewResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var preview = await previewResponse.Content.ReadFromJsonAsync<SegmentPreviewDto>();
        preview.Should().NotBeNull();
        preview!.MatchingCount.Should().BeGreaterThanOrEqualTo(3);
        preview.SampleContacts.Should().HaveCountLessOrEqualTo(10);
        preview.SampleContacts.Should().AllSatisfy(c => c.Industry.Should().Be("Technology"));
    }

    [Fact]
    public async Task Test_Complex_Segment_With_Multiple_Groups()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        // Create varied contacts
        await _client.PostAsJsonAsync("/api/contacts",
            new CreateContactDto("Tech1", "User", $"cplx-tech1-{uniqueId}@test.com", "BigTech", "Technology", null));
        await _client.PostAsJsonAsync("/api/contacts",
            new CreateContactDto("Tech2", "User", $"cplx-tech2-{uniqueId}@test.com", "SmallTech", "Technology", null));
        await _client.PostAsJsonAsync("/api/contacts",
            new CreateContactDto("Health1", "User", $"cplx-health1-{uniqueId}@test.com", "BigHealth", "Healthcare", null));
        await _client.PostAsJsonAsync("/api/contacts",
            new CreateContactDto("Finance1", "User", $"cplx-fin1-{uniqueId}@test.com", "BigBank", "Finance", null));

        // Complex segment:
        // Group 0: industry=Technology AND company contains "Big" (matches Tech1 only)
        // Group 1: industry=Healthcare (matches Health1)
        // Result: Tech1 + Health1 = 2
        var segmentDto = new CreateSegmentDto("Complex Segment", "Multi-group test", new List<SegmentRuleDto>
        {
            new(0, "industry", "equals", "Technology"),
            new(0, "company", "contains", "Big"),
            new(1, "industry", "equals", "Healthcare")
        });
        var segResponse = await _client.PostAsJsonAsync("/api/segments", segmentDto);
        segResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var segment = await segResponse.Content.ReadFromJsonAsync<SegmentDetailDto>();

        var previewResponse = await _client.GetAsync($"/api/segments/{segment!.Id}/preview");
        previewResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var preview = await previewResponse.Content.ReadFromJsonAsync<SegmentPreviewDto>();
        preview.Should().NotBeNull();

        // Should match BigTech (Technology + contains "Big") and BigHealth (Healthcare)
        preview!.MatchingCount.Should().BeGreaterThanOrEqualTo(2);

        // Verify Finance is NOT included
        preview.SampleContacts.Should().NotContain(c => c.Industry == "Finance");
    }
}
