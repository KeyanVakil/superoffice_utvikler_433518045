namespace MarketFlow.Api.Models;

public class SegmentRule
{
    public int Id { get; set; }
    public int SegmentId { get; set; }
    public Segment Segment { get; set; } = null!;
    public int GroupIndex { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
