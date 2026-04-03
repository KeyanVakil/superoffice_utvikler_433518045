using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MarketFlow.Api.Data;
using MarketFlow.Api.Dtos;
using MarketFlow.Api.Models;

namespace MarketFlow.Api.Services;

public class JourneyService
{
    private readonly AppDbContext _db;

    public JourneyService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<JourneyListDto>> GetJourneysAsync()
    {
        return await _db.Journeys
            .Include(j => j.Enrollments)
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => new JourneyListDto(
                j.Id, j.Name, j.TriggerType, j.IsActive, j.CreatedAt,
                j.Enrollments.Count,
                j.Enrollments.Count(e => e.Status == "Completed")))
            .ToListAsync();
    }

    public async Task<JourneyDetailDto?> GetJourneyAsync(int id)
    {
        var journey = await _db.Journeys
            .Include(j => j.Steps)
            .Include(j => j.Enrollments)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (journey is null) return null;

        JsonElement? triggerConfig = null;
        if (journey.TriggerConfig is not null)
        {
            triggerConfig = JsonSerializer.Deserialize<JsonElement>(journey.TriggerConfig);
        }

        var steps = journey.Steps
            .OrderBy(s => s.StepOrder)
            .Select(s =>
            {
                JsonElement? config = null;
                if (s.Config is not null)
                    config = JsonSerializer.Deserialize<JsonElement>(s.Config);
                return new JourneyStepDto(s.Id, s.StepOrder, s.StepType, config, s.TrueNextStepId, s.FalseNextStepId);
            })
            .ToList();

        return new JourneyDetailDto(
            journey.Id, journey.Name, journey.TriggerType, triggerConfig,
            journey.IsActive, journey.CreatedAt, steps,
            journey.Enrollments.Count,
            journey.Enrollments.Count(e => e.Status == "Completed"));
    }

    public async Task<JourneyDetailDto> CreateJourneyAsync(CreateJourneyDto dto)
    {
        var journey = new Journey
        {
            Name = dto.Name,
            TriggerType = dto.TriggerType,
            TriggerConfig = dto.TriggerConfig.HasValue ? dto.TriggerConfig.Value.GetRawText() : null,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };
        _db.Journeys.Add(journey);
        await _db.SaveChangesAsync();

        if (dto.Steps?.Count > 0)
        {
            foreach (var stepDto in dto.Steps.OrderBy(s => s.StepOrder))
            {
                _db.JourneySteps.Add(new JourneyStep
                {
                    JourneyId = journey.Id,
                    StepOrder = stepDto.StepOrder,
                    StepType = stepDto.StepType,
                    Config = stepDto.Config.HasValue ? stepDto.Config.Value.GetRawText() : null
                });
            }
            await _db.SaveChangesAsync();
        }

        return (await GetJourneyAsync(journey.Id))!;
    }

    public async Task<JourneyDetailDto?> UpdateJourneyAsync(int id, CreateJourneyDto dto)
    {
        var journey = await _db.Journeys.Include(j => j.Steps).FirstOrDefaultAsync(j => j.Id == id);
        if (journey is null) return null;

        journey.Name = dto.Name;
        journey.TriggerType = dto.TriggerType;
        journey.TriggerConfig = dto.TriggerConfig.HasValue ? dto.TriggerConfig.Value.GetRawText() : null;

        _db.JourneySteps.RemoveRange(journey.Steps);

        if (dto.Steps?.Count > 0)
        {
            foreach (var stepDto in dto.Steps.OrderBy(s => s.StepOrder))
            {
                _db.JourneySteps.Add(new JourneyStep
                {
                    JourneyId = journey.Id,
                    StepOrder = stepDto.StepOrder,
                    StepType = stepDto.StepType,
                    Config = stepDto.Config.HasValue ? stepDto.Config.Value.GetRawText() : null
                });
            }
        }
        await _db.SaveChangesAsync();

        return (await GetJourneyAsync(id))!;
    }

    public async Task<bool> DeleteJourneyAsync(int id)
    {
        var journey = await _db.Journeys.FindAsync(id);
        if (journey is null) return false;

        _db.Journeys.Remove(journey);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<JourneyDetailDto?> ActivateJourneyAsync(int id)
    {
        var journey = await _db.Journeys.FindAsync(id);
        if (journey is null) return null;

        journey.IsActive = true;
        await _db.SaveChangesAsync();
        return (await GetJourneyAsync(id))!;
    }

    public async Task<JourneyDetailDto?> DeactivateJourneyAsync(int id)
    {
        var journey = await _db.Journeys.FindAsync(id);
        if (journey is null) return null;

        journey.IsActive = false;
        await _db.SaveChangesAsync();
        return (await GetJourneyAsync(id))!;
    }

    public async Task<JourneyEnrollment?> EnrollContactAsync(int journeyId, int contactId)
    {
        var journey = await _db.Journeys.Include(j => j.Steps).FirstOrDefaultAsync(j => j.Id == journeyId);
        if (journey is null) return null;

        if (!journey.IsActive)
            throw new InvalidOperationException("Cannot enroll in an inactive journey.");

        var contact = await _db.Contacts.FindAsync(contactId);
        if (contact is null) return null;

        // Check if already enrolled and active
        var existing = await _db.JourneyEnrollments
            .FirstOrDefaultAsync(e => e.JourneyId == journeyId && e.ContactId == contactId && e.Status == "Active");
        if (existing is not null)
            throw new InvalidOperationException("Contact is already enrolled in this journey.");

        var firstStep = journey.Steps.OrderBy(s => s.StepOrder).FirstOrDefault();

        var enrollment = new JourneyEnrollment
        {
            JourneyId = journeyId,
            ContactId = contactId,
            CurrentStepId = firstStep?.Id,
            Status = "Active",
            EnrolledAt = DateTime.UtcNow
        };
        _db.JourneyEnrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        return enrollment;
    }

    public async Task<bool> ProcessStepAsync(int enrollmentId)
    {
        var enrollment = await _db.JourneyEnrollments
            .Include(e => e.CurrentStep)
            .Include(e => e.Contact)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId);

        if (enrollment is null || enrollment.Status != "Active" || enrollment.CurrentStep is null)
            return false;

        var step = enrollment.CurrentStep;
        string result;
        int? nextStepId;

        switch (step.StepType.ToLower())
        {
            case "send_email":
                // Create a send activity event for this contact
                _db.ActivityEvents.Add(new ActivityEvent
                {
                    ContactId = enrollment.ContactId,
                    EventType = "send",
                    OccurredAt = DateTime.UtcNow,
                    Metadata = step.Config
                });
                result = "email_sent";
                nextStepId = step.TrueNextStepId;
                break;

            case "wait":
                // Check if enough time has passed
                var lastExecution = await _db.JourneyStepExecutions
                    .Where(x => x.EnrollmentId == enrollmentId)
                    .OrderByDescending(x => x.ExecutedAt)
                    .FirstOrDefaultAsync();

                int waitDays = 1;
                if (step.Config is not null)
                {
                    try
                    {
                        var config = JsonSerializer.Deserialize<JsonElement>(step.Config);
                        if (config.TryGetProperty("days", out var daysEl))
                            waitDays = daysEl.GetInt32();
                    }
                    catch { /* use default */ }
                }

                var waitSince = lastExecution?.ExecutedAt ?? enrollment.EnrolledAt;
                if (DateTime.UtcNow < waitSince.AddDays(waitDays))
                {
                    return false; // Still waiting
                }
                result = "wait_completed";
                nextStepId = step.TrueNextStepId;
                break;

            case "condition":
                // Evaluate condition based on contact's activity history
                bool conditionMet = await EvaluateConditionAsync(enrollment.ContactId, step.Config);
                result = conditionMet ? "condition_true" : "condition_false";
                nextStepId = conditionMet ? step.TrueNextStepId : step.FalseNextStepId;
                break;

            default:
                result = "unknown_step_type";
                nextStepId = step.TrueNextStepId;
                break;
        }

        // Record execution
        _db.JourneyStepExecutions.Add(new JourneyStepExecution
        {
            EnrollmentId = enrollmentId,
            StepId = step.Id,
            ExecutedAt = DateTime.UtcNow,
            Result = result
        });

        // Advance to next step
        if (nextStepId.HasValue)
        {
            enrollment.CurrentStepId = nextStepId.Value;
        }
        else
        {
            enrollment.CurrentStepId = null;
            enrollment.Status = "Completed";
            enrollment.CompletedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<JourneyStatsDto?> GetJourneyStatsAsync(int id)
    {
        var journey = await _db.Journeys
            .Include(j => j.Steps)
            .Include(j => j.Enrollments).ThenInclude(e => e.StepExecutions)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (journey is null) return null;

        var enrollments = journey.Enrollments;

        var stepStats = journey.Steps
            .OrderBy(s => s.StepOrder)
            .Select(s => new StepStatsDto(
                s.Id, s.StepOrder, s.StepType,
                enrollments.Count(e => e.StepExecutions.Any(x => x.StepId == s.Id) || e.CurrentStepId == s.Id),
                enrollments.Count(e => e.StepExecutions.Any(x => x.StepId == s.Id))))
            .ToList();

        return new JourneyStatsDto(
            id,
            enrollments.Count,
            enrollments.Count(e => e.Status == "Active"),
            enrollments.Count(e => e.Status == "Completed"),
            enrollments.Count(e => e.Status == "Exited"),
            stepStats);
    }

    private async Task<bool> EvaluateConditionAsync(int contactId, string? config)
    {
        if (config is null) return false;

        try
        {
            var configEl = JsonSerializer.Deserialize<JsonElement>(config);
            var field = configEl.GetProperty("field").GetString() ?? string.Empty;
            var op = configEl.GetProperty("operator").GetString() ?? "equals";
            var value = configEl.GetProperty("value").GetString() ?? string.Empty;

            // Check activity-based conditions
            if (field == "opened_email")
            {
                bool hasOpened = await _db.ActivityEvents
                    .AnyAsync(e => e.ContactId == contactId && e.EventType == "open");
                return (op == "equals" && value == "true") ? hasOpened : !hasOpened;
            }

            if (field == "clicked_link")
            {
                bool hasClicked = await _db.ActivityEvents
                    .AnyAsync(e => e.ContactId == contactId && e.EventType == "click");
                return (op == "equals" && value == "true") ? hasClicked : !hasClicked;
            }

            // Check contact field conditions
            var contact = await _db.Contacts.FindAsync(contactId);
            if (contact is null) return false;

            string fieldValue = field.ToLower() switch
            {
                "industry" => contact.Industry ?? string.Empty,
                "company" => contact.Company ?? string.Empty,
                _ => string.Empty
            };

            return op switch
            {
                "equals" => fieldValue.Equals(value, StringComparison.OrdinalIgnoreCase),
                "not_equals" => !fieldValue.Equals(value, StringComparison.OrdinalIgnoreCase),
                "contains" => fieldValue.Contains(value, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }
}
