namespace MarketFlow.Api.Models;

public class JourneyStep
{
    public int Id { get; set; }
    public int JourneyId { get; set; }
    public Journey Journey { get; set; } = null!;
    public int StepOrder { get; set; }
    public string StepType { get; set; } = string.Empty;
    public string? Config { get; set; }
    public int? TrueNextStepId { get; set; }
    public JourneyStep? TrueNextStep { get; set; }
    public int? FalseNextStepId { get; set; }
    public JourneyStep? FalseNextStep { get; set; }
}
