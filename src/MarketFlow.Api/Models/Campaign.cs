namespace MarketFlow.Api.Models;

public class Campaign
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public int? SegmentId { get; set; }
    public Segment? Segment { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ActivityEvent> ActivityEvents { get; set; } = new List<ActivityEvent>();
}
