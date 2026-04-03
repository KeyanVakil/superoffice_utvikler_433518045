namespace MarketFlow.Api.Models;

public class JourneyStepExecution
{
    public int Id { get; set; }
    public int EnrollmentId { get; set; }
    public JourneyEnrollment Enrollment { get; set; } = null!;
    public int StepId { get; set; }
    public JourneyStep Step { get; set; } = null!;
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public string? Result { get; set; }
}
