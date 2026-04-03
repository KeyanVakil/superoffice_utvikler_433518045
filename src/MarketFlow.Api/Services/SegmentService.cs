using Microsoft.EntityFrameworkCore;
using MarketFlow.Api.Data;
using MarketFlow.Api.Dtos;
using MarketFlow.Api.Models;

namespace MarketFlow.Api.Services;

public class SegmentService
{
    private readonly AppDbContext _db;

    public SegmentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<SegmentListDto>> GetSegmentsAsync()
    {
        return await _db.Segments
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SegmentListDto(s.Id, s.Name, s.Description, s.CreatedAt))
            .ToListAsync();
    }

    public async Task<SegmentDetailDto?> GetSegmentAsync(int id)
    {
        var segment = await _db.Segments
            .Include(s => s.Rules)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (segment is null) return null;

        var rules = segment.Rules
            .OrderBy(r => r.GroupIndex).ThenBy(r => r.SortOrder)
            .Select(r => new SegmentRuleDto(r.GroupIndex, r.Field, r.Operator, r.Value))
            .ToList();

        return new SegmentDetailDto(segment.Id, segment.Name, segment.Description, segment.CreatedAt, rules);
    }

    public async Task<SegmentDetailDto> CreateSegmentAsync(CreateSegmentDto dto)
    {
        var segment = new Segment
        {
            Name = dto.Name,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow
        };
        _db.Segments.Add(segment);
        await _db.SaveChangesAsync();

        if (dto.Rules?.Count > 0)
        {
            int sortOrder = 0;
            foreach (var rule in dto.Rules)
            {
                _db.SegmentRules.Add(new SegmentRule
                {
                    SegmentId = segment.Id,
                    GroupIndex = rule.GroupIndex,
                    Field = rule.Field,
                    Operator = rule.Operator,
                    Value = rule.Value,
                    SortOrder = sortOrder++
                });
            }
            await _db.SaveChangesAsync();
        }

        return (await GetSegmentAsync(segment.Id))!;
    }

    public async Task<SegmentDetailDto?> UpdateSegmentAsync(int id, CreateSegmentDto dto)
    {
        var segment = await _db.Segments.Include(s => s.Rules).FirstOrDefaultAsync(s => s.Id == id);
        if (segment is null) return null;

        segment.Name = dto.Name;
        segment.Description = dto.Description;

        _db.SegmentRules.RemoveRange(segment.Rules);

        if (dto.Rules?.Count > 0)
        {
            int sortOrder = 0;
            foreach (var rule in dto.Rules)
            {
                _db.SegmentRules.Add(new SegmentRule
                {
                    SegmentId = segment.Id,
                    GroupIndex = rule.GroupIndex,
                    Field = rule.Field,
                    Operator = rule.Operator,
                    Value = rule.Value,
                    SortOrder = sortOrder++
                });
            }
        }
        await _db.SaveChangesAsync();

        return (await GetSegmentAsync(id))!;
    }

    public async Task<bool> DeleteSegmentAsync(int id)
    {
        var segment = await _db.Segments.FindAsync(id);
        if (segment is null) return false;

        _db.Segments.Remove(segment);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<SegmentPreviewDto?> PreviewSegmentAsync(int id)
    {
        var segment = await _db.Segments.Include(s => s.Rules).FirstOrDefaultAsync(s => s.Id == id);
        if (segment is null) return null;

        var matchingContacts = await EvaluateSegmentAsync(id);
        var cutoff = DateTime.UtcNow.AddDays(-90);

        var sampleContacts = matchingContacts.Take(10).ToList();

        var samples = sampleContacts.Select(c =>
        {
            var score = ComputeEngagementScore(c.ActivityEvents ?? new List<ActivityEvent>(), cutoff);
            var lastActivity = (c.ActivityEvents ?? new List<ActivityEvent>())
                .OrderByDescending(e => e.OccurredAt).FirstOrDefault()?.OccurredAt;
            var tags = (c.Tags ?? new List<ContactTag>())
                .Where(ct => ct.Tag != null).Select(ct => ct.Tag.Name).ToArray();
            return new ContactListDto(c.Id, c.FirstName, c.LastName, c.Email, c.Company, c.Industry,
                tags, score, lastActivity);
        }).ToList();

        return new SegmentPreviewDto(id, matchingContacts.Count, samples);
    }

    public async Task<SegmentPreviewDto> PreviewRulesAsync(List<SegmentRuleDto> ruleDtos)
    {
        if (ruleDtos.Count == 0)
            return new SegmentPreviewDto(0, 0, new List<ContactListDto>());

        var rules = ruleDtos.Select(r => new SegmentRule
        {
            GroupIndex = r.GroupIndex,
            Field = r.Field,
            Operator = r.Operator,
            Value = r.Value
        }).ToList();

        var groups = rules.GroupBy(r => r.GroupIndex).ToList();

        var allContacts = await _db.Contacts
            .Include(c => c.Tags).ThenInclude(ct => ct.Tag)
            .Include(c => c.ActivityEvents)
            .ToListAsync();

        var cutoff = DateTime.UtcNow.AddDays(-90);
        var matchingContactIds = new HashSet<int>();

        foreach (var group in groups)
        {
            var groupRules = group.ToList();
            var groupMatches = allContacts.Where(contact =>
                groupRules.All(rule => EvaluateRule(contact, rule, cutoff))
            ).Select(c => c.Id);

            foreach (var id in groupMatches)
                matchingContactIds.Add(id);
        }

        var matchingContacts = allContacts.Where(c => matchingContactIds.Contains(c.Id)).ToList();

        var samples = matchingContacts.Take(10).Select(c =>
        {
            var score = ComputeEngagementScore(c.ActivityEvents ?? new List<ActivityEvent>(), cutoff);
            var lastActivity = (c.ActivityEvents ?? new List<ActivityEvent>())
                .OrderByDescending(e => e.OccurredAt).FirstOrDefault()?.OccurredAt;
            var tags = (c.Tags ?? new List<ContactTag>())
                .Where(ct => ct.Tag != null).Select(ct => ct.Tag.Name).ToArray();
            return new ContactListDto(c.Id, c.FirstName, c.LastName, c.Email, c.Company, c.Industry,
                tags, score, lastActivity);
        }).ToList();

        return new SegmentPreviewDto(0, matchingContacts.Count, samples);
    }

    public async Task<List<Contact>> EvaluateSegmentAsync(int segmentId)
    {
        var rules = await _db.SegmentRules
            .Where(r => r.SegmentId == segmentId)
            .OrderBy(r => r.GroupIndex).ThenBy(r => r.SortOrder)
            .ToListAsync();

        if (rules.Count == 0)
            return new List<Contact>();

        // Group rules by GroupIndex. Rules in same group are AND'd, groups are OR'd.
        var groups = rules.GroupBy(r => r.GroupIndex).ToList();

        // Load all contacts with their tags and events for evaluation
        var allContacts = await _db.Contacts
            .Include(c => c.Tags).ThenInclude(ct => ct.Tag)
            .Include(c => c.ActivityEvents)
            .ToListAsync();

        var cutoff = DateTime.UtcNow.AddDays(-90);
        var matchingContactIds = new HashSet<int>();

        // Each group is OR'd: if a contact matches ANY group, it's included
        foreach (var group in groups)
        {
            var groupRules = group.ToList();
            var groupMatches = allContacts.Where(contact =>
            {
                // All rules in a group must match (AND)
                return groupRules.All(rule => EvaluateRule(contact, rule, cutoff));
            }).Select(c => c.Id);

            foreach (var id in groupMatches)
                matchingContactIds.Add(id);
        }

        return allContacts.Where(c => matchingContactIds.Contains(c.Id)).ToList();
    }

    private static bool EvaluateRule(Contact contact, SegmentRule rule, DateTime cutoff)
    {
        var field = rule.Field.ToLower();
        var op = rule.Operator.ToLower();
        var value = rule.Value;

        switch (field)
        {
            case "industry":
                return EvaluateStringOp(contact.Industry ?? string.Empty, op, value);
            case "company":
                return EvaluateStringOp(contact.Company ?? string.Empty, op, value);
            case "firstname":
                return EvaluateStringOp(contact.FirstName, op, value);
            case "lastname":
                return EvaluateStringOp(contact.LastName, op, value);
            case "email":
                return EvaluateStringOp(contact.Email, op, value);
            case "tag":
                var tagNames = (contact.Tags ?? new List<ContactTag>())
                    .Where(ct => ct.Tag != null)
                    .Select(ct => ct.Tag.Name.ToLower())
                    .ToList();
                return op switch
                {
                    "equals" or "has" => tagNames.Contains(value.ToLower()),
                    "not_equals" or "not_has" => !tagNames.Contains(value.ToLower()),
                    "contains" => tagNames.Any(t => t.Contains(value.ToLower())),
                    _ => false
                };
            case "engagementscore":
                var events = contact.ActivityEvents ?? new List<ActivityEvent>();
                var recentEvents = events.Where(e => e.OccurredAt >= cutoff).ToList();
                double score = recentEvents.Count(e => e.EventType == "open") * 2
                             + recentEvents.Count(e => e.EventType == "click") * 3;
                if (!double.TryParse(value, out var threshold)) return false;
                return op switch
                {
                    "greaterthan" or "greater_than" => score > threshold,
                    "lessthan" or "less_than" => score < threshold,
                    "equals" => Math.Abs(score - threshold) < 0.01,
                    _ => false
                };
            case "lastactivity":
                var lastEvent = (contact.ActivityEvents ?? new List<ActivityEvent>())
                    .OrderByDescending(e => e.OccurredAt).FirstOrDefault();
                if (lastEvent is null) return op == "before"; // no activity is "before" anything
                if (!int.TryParse(value, out var days)) return false;
                var compareDate = DateTime.UtcNow.AddDays(-days);
                return op switch
                {
                    "before" => lastEvent.OccurredAt < compareDate,
                    "after" or "within_days" => lastEvent.OccurredAt > compareDate,
                    _ => false
                };
            default:
                return false;
        }
    }

    private static bool EvaluateStringOp(string fieldValue, string op, string value)
    {
        return op switch
        {
            "equals" => fieldValue.Equals(value, StringComparison.OrdinalIgnoreCase),
            "not_equals" => !fieldValue.Equals(value, StringComparison.OrdinalIgnoreCase),
            "contains" => fieldValue.Contains(value, StringComparison.OrdinalIgnoreCase),
            "not_contains" => !fieldValue.Contains(value, StringComparison.OrdinalIgnoreCase),
            "starts_with" => fieldValue.StartsWith(value, StringComparison.OrdinalIgnoreCase),
            "ends_with" => fieldValue.EndsWith(value, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static double ComputeEngagementScore(IEnumerable<ActivityEvent> events, DateTime cutoff)
    {
        var recentEvents = events.Where(e => e.OccurredAt >= cutoff).ToList();
        int rawScore = recentEvents.Count(e => e.EventType == "open") * 2
                     + recentEvents.Count(e => e.EventType == "click") * 3;
        return Math.Min(rawScore, 100);
    }
}
