using FluentAssertions;
using MarketFlow.Api.Services;
using Xunit;

namespace MarketFlow.Tests.Unit;

public class AiSuggestionServiceTests
{
    private AiSuggestionService CreateService()
    {
        var db = TestDbContextFactory.Create();
        return new AiSuggestionService(db);
    }

    [Fact]
    public void Test_Suggestions_Add_Personalization()
    {
        var service = CreateService();

        // Subject without personalization token
        var suggestions = service.GenerateSubjectSuggestions("Check out our new product", null);

        // At least one suggestion should include {{firstName}} personalization
        suggestions.Suggestions.Should().Contain(s => s.Subject.Contains("{{firstName}}"));
    }

    [Fact]
    public void Test_Suggestions_Add_Numbers()
    {
        var service = CreateService();

        var suggestions = service.GenerateSubjectSuggestions("Ways to improve your workflow", null);

        // At least one suggestion should include a number for specificity
        suggestions.Suggestions.Should().Contain(s =>
            s.Subject.Any(char.IsDigit));
    }

    [Fact]
    public void Test_Suggestions_Shorten_Long_Subject()
    {
        var service = CreateService();

        var longSubject = "This is an extremely long subject line that goes on and on and contains way too many words for an effective email campaign subject";
        var suggestions = service.GenerateSubjectSuggestions(longSubject, null);

        // At least one suggestion should be shorter than the original
        suggestions.Suggestions.Should().Contain(s => s.Subject.Length < longSubject.Length);
    }

    [Fact]
    public void Test_Always_Returns_Three_Suggestions()
    {
        var service = CreateService();

        var suggestions1 = service.GenerateSubjectSuggestions("Simple subject", null);
        var suggestions2 = service.GenerateSubjectSuggestions("Another very different subject about sales", "product launch");
        var suggestions3 = service.GenerateSubjectSuggestions("{{firstName}}, check this out!", null);

        suggestions1.Suggestions.Should().HaveCount(3);
        suggestions2.Suggestions.Should().HaveCount(3);
        suggestions3.Suggestions.Should().HaveCount(3);

        // Each suggestion should have a non-empty subject and reason
        suggestions1.Suggestions.Should().AllSatisfy(s =>
        {
            s.Subject.Should().NotBeNullOrWhiteSpace();
            s.Reason.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public void Test_Send_Time_Returns_Valid_Recommendation()
    {
        var service = CreateService();

        var recommendation = service.GetSendTimeRecommendation(null);

        recommendation.RecommendedHour.Should().BeInRange(0, 23);
        recommendation.RecommendedDay.Should().BeOneOf(
            "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday");
        recommendation.Reason.Should().NotBeNullOrWhiteSpace();
    }
}
