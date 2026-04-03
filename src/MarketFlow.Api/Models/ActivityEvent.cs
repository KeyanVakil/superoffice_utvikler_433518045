namespace MarketFlow.Api.Models;

public class ActivityEvent
{
    public int Id { get; set; }
    public int ContactId { get; set; }
    public Contact Contact { get; set; } = null!;
    public int? CampaignId { get; set; }
    public Campaign? Campaign { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? Metadata { get; set; }
}
