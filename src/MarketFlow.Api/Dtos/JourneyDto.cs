using System.Text.Json;

namespace MarketFlow.Api.Dtos;

public record JourneyListDto(
    int Id,
    string Name,
    string TriggerType,
    bool IsActive,
    DateTime CreatedAt,
    int EnrolledCount,
    int CompletedCount
);

public record JourneyDetailDto(
    int Id,
    string Name,
    string TriggerType,
    JsonElement? TriggerConfig,
    bool IsActive,
    DateTime CreatedAt,
    List<JourneyStepDto> Steps,
    int EnrolledCount,
    int CompletedCount
);

public record CreateJourneyDto(
    string Name,
    string TriggerType,
    JsonElement? TriggerConfig,
    List<CreateJourneyStepDto>? Steps
);

public record JourneyStepDto(
    int Id,
    int StepOrder,
    string StepType,
    JsonElement? Config,
    int? TrueNextStepId,
    int? FalseNextStepId
);

public record CreateJourneyStepDto(
    int StepOrder,
    string StepType,
    JsonElement? Config
);

public record JourneyStatsDto(
    int JourneyId,
    int TotalEnrolled,
    int Active,
    int Completed,
    int Exited,
    List<StepStatsDto> StepStats
);

public record StepStatsDto(
    int StepId,
    int StepOrder,
    string StepType,
    int Reached,
    int Completed
);
