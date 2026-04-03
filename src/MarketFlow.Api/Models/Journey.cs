namespace MarketFlow.Api.Models;

public class Journey
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TriggerType { get; set; } = string.Empty;
    public string? TriggerConfig { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<JourneyStep> Steps { get; set; } = new List<JourneyStep>();
    public ICollection<JourneyEnrollment> Enrollments { get; set; } = new List<JourneyEnrollment>();
}
