namespace MarketFlow.Api.Dtos;

public record SegmentListDto(
    int Id,
    string Name,
    string? Description,
    DateTime CreatedAt
);

public record SegmentDetailDto(
    int Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    List<SegmentRuleDto> Rules
);

public record CreateSegmentDto(
    string Name,
    string? Description,
    List<SegmentRuleDto>? Rules
);

public record SegmentRuleDto(
    int GroupIndex,
    string Field,
    string Operator,
    string Value
);

public record SegmentPreviewDto(
    int SegmentId,
    int MatchingCount,
    List<ContactListDto> SampleContacts
);
