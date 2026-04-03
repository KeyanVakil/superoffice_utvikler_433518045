using System.Text.Json;
using FluentAssertions;
using MarketFlow.Api.Models;
using MarketFlow.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MarketFlow.Tests.Unit;

public class JourneyServiceTests
{
    private async Task<(JourneyService Service, string DbName)> SetupJourneyWithSteps()
    {
        var dbName = Guid.NewGuid().ToString();
        var db = TestDbContextFactory.Create(dbName);

        var contact = new Contact { FirstName = "Alice", LastName = "Smith", Email = "alice@test.com", Industry = "Technology" };
        db.Contacts.Add(contact);
        await db.SaveChangesAsync();

        var journey = new Journey { Name = "Welcome Journey", TriggerType = "manual", IsActive = true };
        db.Journeys.Add(journey);
        await db.SaveChangesAsync();

        var step1 = new JourneyStep
        {
            JourneyId = journey.Id,
            StepOrder = 1,
            StepType = "send_email",
            Config = JsonSerializer.Serialize(new { subject = "Welcome!", body = "Hello {{firstName}}" })
        };
        db.JourneySteps.Add(step1);
        await db.SaveChangesAsync();

        var step2 = new JourneyStep
        {
            JourneyId = journey.Id,
            StepOrder = 2,
            StepType = "wait",
            Config = JsonSerializer.Serialize(new { days = 1 })
        };
        db.JourneySteps.Add(step2);
        await db.SaveChangesAsync();

        // Update step1 to point to step2
        step1.TrueNextStepId = step2.Id;
        await db.SaveChangesAsync();

        return (new JourneyService(db), dbName);
    }

    [Fact]
    public async Task Test_Enroll_Contact_Creates_Enrollment_At_First_Step()
    {
        var (service, dbName) = await SetupJourneyWithSteps();
        using var db = TestDbContextFactory.Create(dbName);

        var journey = await db.Journeys.Include(j => j.Steps).FirstAsync();
        var contact = await db.Contacts.FirstAsync();
        var firstStep = journey.Steps.OrderBy(s => s.StepOrder).First();

        var svc = new JourneyService(db);
        var enrollment = await svc.EnrollContactAsync(journey.Id, contact.Id);

        enrollment.Should().NotBeNull();
        enrollment.JourneyId.Should().Be(journey.Id);
        enrollment.ContactId.Should().Be(contact.Id);
        enrollment.CurrentStepId.Should().Be(firstStep.Id);
        enrollment.Status.Should().Be("Active");
        enrollment.EnrolledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Test_Process_SendEmail_Step_Creates_Event()
    {
        var (service, dbName) = await SetupJourneyWithSteps();
        using var db = TestDbContextFactory.Create(dbName);

        var journey = await db.Journeys.Include(j => j.Steps).FirstAsync();
        var contact = await db.Contacts.FirstAsync();
        var firstStep = journey.Steps.OrderBy(s => s.StepOrder).First();

        var svc = new JourneyService(db);
        var enrollment = await svc.EnrollContactAsync(journey.Id, contact.Id);

        await svc.ProcessStepAsync(enrollment.Id);

        // Verify an activity event was created for the send_email step
        var events = await db.ActivityEvents.Where(e => e.ContactId == contact.Id).ToListAsync();
        events.Should().HaveCount(1);
        events.First().EventType.Should().Be("send");

        // Verify execution recorded
        var executions = await db.JourneyStepExecutions
            .Where(x => x.EnrollmentId == enrollment.Id)
            .ToListAsync();
        executions.Should().HaveCount(1);
        executions.First().StepId.Should().Be(firstStep.Id);
    }

    [Fact]
    public async Task Test_Process_Wait_Step_Checks_Time_Elapsed()
    {
        var dbName = Guid.NewGuid().ToString();
        var db = TestDbContextFactory.Create(dbName);

        var contact = new Contact { FirstName = "Bob", LastName = "Jones", Email = "bob@test.com" };
        db.Contacts.Add(contact);
        await db.SaveChangesAsync();

        var journey = new Journey { Name = "Wait Journey", TriggerType = "manual", IsActive = true };
        db.Journeys.Add(journey);
        await db.SaveChangesAsync();

        var waitStep = new JourneyStep
        {
            JourneyId = journey.Id,
            StepOrder = 1,
            StepType = "wait",
            Config = JsonSerializer.Serialize(new { days = 1 })
        };
        db.JourneySteps.Add(waitStep);
        await db.SaveChangesAsync();

        var service = new JourneyService(db);
        var enrollment = await service.EnrollContactAsync(journey.Id, contact.Id);

        // Processing a wait step that just started should not advance (time not elapsed)
        await service.ProcessStepAsync(enrollment.Id);

        var updated = await db.JourneyEnrollments.FindAsync(enrollment.Id);
        // Should still be at the wait step since 1 day hasn't passed
        updated!.CurrentStepId.Should().Be(waitStep.Id);
        updated.Status.Should().Be("Active");
    }

    [Fact]
    public async Task Test_Process_Condition_Step_True_Branch()
    {
        var dbName = Guid.NewGuid().ToString();
        var db = TestDbContextFactory.Create(dbName);

        var contact = new Contact { FirstName = "Alice", LastName = "Smith", Email = "alice@test.com", Industry = "Technology" };
        db.Contacts.Add(contact);
        await db.SaveChangesAsync();

        var journey = new Journey { Name = "Condition Journey", TriggerType = "manual", IsActive = true };
        db.Journeys.Add(journey);
        await db.SaveChangesAsync();

        var trueStep = new JourneyStep
        {
            JourneyId = journey.Id,
            StepOrder = 2,
            StepType = "send_email",
            Config = JsonSerializer.Serialize(new { subject = "You match!", body = "Hello" })
        };
        var falseStep = new JourneyStep
        {
            JourneyId = journey.Id,
            StepOrder = 3,
            StepType = "send_email",
            Config = JsonSerializer.Serialize(new { subject = "No match", body = "Sorry" })
        };
        db.JourneySteps.AddRange(trueStep, falseStep);
        await db.SaveChangesAsync();

        // Condition: industry equals Technology
        var conditionStep = new JourneyStep
        {
            JourneyId = journey.Id,
            StepOrder = 1,
            StepType = "condition",
            Config = JsonSerializer.Serialize(new { field = "industry", @operator = "equals", value = "Technology" }),
            TrueNextStepId = trueStep.Id,
            FalseNextStepId = falseStep.Id
        };
        db.JourneySteps.Add(conditionStep);
        await db.SaveChangesAsync();

        var service = new JourneyService(db);
        var enrollment = new JourneyEnrollment
        {
            JourneyId = journey.Id,
            ContactId = contact.Id,
            CurrentStepId = conditionStep.Id,
            Status = "Active"
        };
        db.JourneyEnrollments.Add(enrollment);
        await db.SaveChangesAsync();

        await service.ProcessStepAsync(enrollment.Id);

        var updated = await db.JourneyEnrollments.FindAsync(enrollment.Id);
        // Alice is in Technology -> should go to true branch
        updated!.CurrentStepId.Should().Be(trueStep.Id);
    }

    [Fact]
    public async Task Test_Process_Condition_Step_False_Branch()
    {
        var dbName = Guid.NewGuid().ToString();
        var db = TestDbContextFactory.Create(dbName);

        var contact = new Contact { FirstName = "Bob", LastName = "Jones", Email = "bob@test.com", Industry = "Finance" };
        db.Contacts.Add(contact);
        await db.SaveChangesAsync();

        var journey = new Journey { Name = "Condition Journey", TriggerType = "manual", IsActive = true };
        db.Journeys.Add(journey);
        await db.SaveChangesAsync();

        var trueStep = new JourneyStep
        {
            JourneyId = journey.Id,
            StepOrder = 2,
            StepType = "send_email",
            Config = JsonSerializer.Serialize(new { subject = "Match", body = "Hello" })
        };
        var falseStep = new JourneyStep
        {
            JourneyId = journey.Id,
            StepOrder = 3,
            StepType = "send_email",
            Config = JsonSerializer.Serialize(new { subject = "No match", body = "Sorry" })
        };
        db.JourneySteps.AddRange(trueStep, falseStep);
        await db.SaveChangesAsync();

        var conditionStep = new JourneyStep
        {
            JourneyId = journey.Id,
            StepOrder = 1,
            StepType = "condition",
            Config = JsonSerializer.Serialize(new { field = "industry", @operator = "equals", value = "Technology" }),
            TrueNextStepId = trueStep.Id,
            FalseNextStepId = falseStep.Id
        };
        db.JourneySteps.Add(conditionStep);
        await db.SaveChangesAsync();

        var service = new JourneyService(db);
        var enrollment = new JourneyEnrollment
        {
            JourneyId = journey.Id,
            ContactId = contact.Id,
            CurrentStepId = conditionStep.Id,
            Status = "Active"
        };
        db.JourneyEnrollments.Add(enrollment);
        await db.SaveChangesAsync();

        await service.ProcessStepAsync(enrollment.Id);

        var updated = await db.JourneyEnrollments.FindAsync(enrollment.Id);
        // Bob is in Finance, not Technology -> should go to false branch
        updated!.CurrentStepId.Should().Be(falseStep.Id);
    }

    [Fact]
    public async Task Test_Activate_Deactivate_Journey()
    {
        var dbName = Guid.NewGuid().ToString();
        var db = TestDbContextFactory.Create(dbName);

        var journey = new Journey { Name = "Toggle Test", TriggerType = "manual", IsActive = false };
        db.Journeys.Add(journey);
        await db.SaveChangesAsync();

        var service = new JourneyService(db);

        // Activate
        await service.ActivateJourneyAsync(journey.Id);
        var activated = await db.Journeys.FindAsync(journey.Id);
        activated!.IsActive.Should().BeTrue();

        // Deactivate
        await service.DeactivateJourneyAsync(journey.Id);
        var deactivated = await db.Journeys.FindAsync(journey.Id);
        deactivated!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Test_Cannot_Enroll_In_Inactive_Journey()
    {
        var dbName = Guid.NewGuid().ToString();
        var db = TestDbContextFactory.Create(dbName);

        var contact = new Contact { FirstName = "Alice", LastName = "Smith", Email = "alice@test.com" };
        db.Contacts.Add(contact);

        var journey = new Journey { Name = "Inactive Journey", TriggerType = "manual", IsActive = false };
        db.Journeys.Add(journey);
        await db.SaveChangesAsync();

        var step = new JourneyStep { JourneyId = journey.Id, StepOrder = 1, StepType = "send_email" };
        db.JourneySteps.Add(step);
        await db.SaveChangesAsync();

        var service = new JourneyService(db);

        var act = () => service.EnrollContactAsync(journey.Id, contact.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*inactive*");
    }

    [Fact]
    public async Task Test_Duplicate_Enrollment_Prevented()
    {
        var (service, dbName) = await SetupJourneyWithSteps();
        using var db = TestDbContextFactory.Create(dbName);

        var journey = await db.Journeys.FirstAsync();
        var contact = await db.Contacts.FirstAsync();

        var svc = new JourneyService(db);

        // First enrollment should succeed
        var enrollment1 = await svc.EnrollContactAsync(journey.Id, contact.Id);
        enrollment1.Should().NotBeNull();

        // Second enrollment should fail
        var act = () => svc.EnrollContactAsync(journey.Id, contact.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already enrolled*");
    }
}
