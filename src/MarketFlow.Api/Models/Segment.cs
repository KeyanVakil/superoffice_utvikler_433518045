namespace MarketFlow.Api.Models;

public class Segment
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SegmentRule> Rules { get; set; } = new List<SegmentRule>();
}
