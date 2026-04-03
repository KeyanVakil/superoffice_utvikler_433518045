using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using MarketFlow.Api.Dtos;
using Xunit;

namespace MarketFlow.Tests.Integration;

public class ContactsApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ContactsApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Test_Create_And_Get_Contact()
    {
        var createDto = new CreateContactDto(
            "Alice", "Smith", $"alice-{Guid.NewGuid():N}@test.com", "TechCorp", "Technology", new[] { "vip" });

        var createResponse = await _client.PostAsJsonAsync("/api/contacts", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ContactDetailDto>();
        created.Should().NotBeNull();
        created!.FirstName.Should().Be("Alice");
        created.LastName.Should().Be("Smith");
        created.Company.Should().Be("TechCorp");
        created.Industry.Should().Be("Technology");
        created.Tags.Should().Contain("vip");

        // GET the same contact
        var getResponse = await _client.GetAsync($"/api/contacts/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await getResponse.Content.ReadFromJsonAsync<ContactDetailDto>();
        fetched.Should().NotBeNull();
        fetched!.Id.Should().Be(created.Id);
        fetched.FirstName.Should().Be("Alice");
        fetched.Email.Should().Be(created.Email);
    }

    [Fact]
    public async Task Test_List_Contacts_With_Pagination()
    {
        // Create 25 contacts
        for (int i = 1; i <= 25; i++)
        {
            var dto = new CreateContactDto(
                $"User{i}", "Test", $"pagination-{Guid.NewGuid():N}@test.com", "Company", "Tech", null);
            var resp = await _client.PostAsJsonAsync("/api/contacts", dto);
            resp.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
        }

        // Page 1 with default page size (should return up to 25)
        var page1Response = await _client.GetAsync("/api/contacts?page=1&pageSize=20");
        page1Response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page1 = await page1Response.Content.ReadFromJsonAsync<PagedResult<ContactListDto>>();
        page1.Should().NotBeNull();
        page1!.Items.Should().HaveCount(20);
        page1.TotalCount.Should().BeGreaterThanOrEqualTo(25);

        // Page 2
        var page2Response = await _client.GetAsync("/api/contacts?page=2&pageSize=20");
        page2Response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page2 = await page2Response.Content.ReadFromJsonAsync<PagedResult<ContactListDto>>();
        page2.Should().NotBeNull();
        page2!.Items.Should().HaveCountGreaterThanOrEqualTo(5);
    }

    [Fact]
    public async Task Test_Search_Contacts()
    {
        var uniquePrefix = Guid.NewGuid().ToString("N")[..8];
        var dto1 = new CreateContactDto(
            $"Searchable{uniquePrefix}", "Person", $"search1-{uniquePrefix}@test.com", "FindMe Corp", "Tech", null);
        var dto2 = new CreateContactDto(
            "Other", "Person", $"other-{uniquePrefix}@test.com", "Hidden Corp", "Finance", null);

        await _client.PostAsJsonAsync("/api/contacts", dto1);
        await _client.PostAsJsonAsync("/api/contacts", dto2);

        var searchResponse = await _client.GetAsync($"/api/contacts?search=Searchable{uniquePrefix}");
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var results = await searchResponse.Content.ReadFromJsonAsync<PagedResult<ContactListDto>>();
        results.Should().NotBeNull();
        results!.Items.Should().HaveCount(1);
        results.Items.First().FirstName.Should().Contain("Searchable");
    }

    [Fact]
    public async Task Test_Delete_Contact()
    {
        var dto = new CreateContactDto(
            "ToDelete", "Person", $"delete-{Guid.NewGuid():N}@test.com", null, null, null);
        var createResponse = await _client.PostAsJsonAsync("/api/contacts", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<ContactDetailDto>();

        var deleteResponse = await _client.DeleteAsync($"/api/contacts/{created!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/contacts/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Test_Import_Csv()
    {
        var csvContent = @"FirstName,LastName,Email,Company,Industry,Tags
Import1,User,import1-" + Guid.NewGuid().ToString("N") + @"@test.com,ImportCo,Tech,lead;hot
Import2,User,import2-" + Guid.NewGuid().ToString("N") + @"@test.com,ImportCo,Finance,";

        using var content = new MultipartFormDataContent();
        var csvBytes = Encoding.UTF8.GetBytes(csvContent);
        var fileContent = new ByteArrayContent(csvBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "contacts.csv");

        var response = await _client.PostAsync("/api/contacts/import", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ContactImportResultDto>();
        result.Should().NotBeNull();
        result!.Imported.Should().Be(2);
        result.Errors.Should().BeEmpty();
    }
}
