namespace MarketFlow.Api.Models;

public class JourneyEnrollment
{
    public int Id { get; set; }
    public int JourneyId { get; set; }
    public Journey Journey { get; set; } = null!;
    public int ContactId { get; set; }
    public Contact Contact { get; set; } = null!;
    public int? CurrentStepId { get; set; }
    public JourneyStep? CurrentStep { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public ICollection<JourneyStepExecution> StepExecutions { get; set; } = new List<JourneyStepExecution>();
}
