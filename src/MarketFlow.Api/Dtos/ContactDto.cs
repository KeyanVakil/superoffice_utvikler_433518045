namespace MarketFlow.Api.Dtos;

public record ContactListDto(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string? Company,
    string? Industry,
    string[] Tags,
    double EngagementScore,
    DateTime? LastActivityAt
);

public record ContactDetailDto(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string? Company,
    string? Industry,
    string[] Tags,
    double EngagementScore,
    DateTime? LastActivityAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<ActivityEventDto> ActivityTimeline
);

public record ActivityEventDto(
    int Id,
    string EventType,
    DateTime OccurredAt,
    int? CampaignId,
    string? CampaignName,
    string? Metadata
);

public record CreateContactDto(
    string FirstName,
    string LastName,
    string Email,
    string? Company,
    string? Industry,
    string[]? Tags
);

public record UpdateContactDto(
    string FirstName,
    string LastName,
    string Email,
    string? Company,
    string? Industry,
    string[]? Tags
);

public record ContactImportResultDto(
    int Imported,
    int Skipped,
    List<string> Errors
);

public record PagedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);
