using Microsoft.EntityFrameworkCore;
using MarketFlow.Api.Models;

namespace MarketFlow.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ContactTag> ContactTags => Set<ContactTag>();
    public DbSet<Segment> Segments => Set<Segment>();
    public DbSet<SegmentRule> SegmentRules => Set<SegmentRule>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<ActivityEvent> ActivityEvents => Set<ActivityEvent>();
    public DbSet<Journey> Journeys => Set<Journey>();
    public DbSet<JourneyStep> JourneySteps => Set<JourneyStep>();
    public DbSet<JourneyEnrollment> JourneyEnrollments => Set<JourneyEnrollment>();
    public DbSet<JourneyStepExecution> JourneyStepExecutions => Set<JourneyStepExecution>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ContactTag composite key
        modelBuilder.Entity<ContactTag>()
            .HasKey(ct => new { ct.ContactId, ct.TagId });

        modelBuilder.Entity<ContactTag>()
            .HasOne(ct => ct.Contact)
            .WithMany(c => c.Tags)
            .HasForeignKey(ct => ct.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ContactTag>()
            .HasOne(ct => ct.Tag)
            .WithMany(t => t.Contacts)
            .HasForeignKey(ct => ct.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        // Contact indexes
        modelBuilder.Entity<Contact>()
            .HasIndex(c => c.Email)
            .IsUnique();

        modelBuilder.Entity<Contact>()
            .HasIndex(c => c.Industry);

        modelBuilder.Entity<Contact>()
            .HasIndex(c => c.Company);

        // Tag unique name
        modelBuilder.Entity<Tag>()
            .HasIndex(t => t.Name)
            .IsUnique();

        // SegmentRule
        modelBuilder.Entity<SegmentRule>()
            .HasOne(r => r.Segment)
            .WithMany(s => s.Rules)
            .HasForeignKey(r => r.SegmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Campaign -> Segment
        modelBuilder.Entity<Campaign>()
            .HasOne(c => c.Segment)
            .WithMany()
            .HasForeignKey(c => c.SegmentId)
            .OnDelete(DeleteBehavior.SetNull);

        // ActivityEvent relationships
        modelBuilder.Entity<ActivityEvent>()
            .HasOne(e => e.Contact)
            .WithMany(c => c.ActivityEvents)
            .HasForeignKey(e => e.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ActivityEvent>()
            .HasOne(e => e.Campaign)
            .WithMany(c => c.ActivityEvents)
            .HasForeignKey(e => e.CampaignId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ActivityEvent>()
            .HasIndex(e => e.EventType);

        modelBuilder.Entity<ActivityEvent>()
            .HasIndex(e => e.OccurredAt);

        // Journey relationships
        modelBuilder.Entity<JourneyStep>()
            .HasOne(s => s.Journey)
            .WithMany(j => j.Steps)
            .HasForeignKey(s => s.JourneyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JourneyStep>()
            .HasOne(s => s.TrueNextStep)
            .WithMany()
            .HasForeignKey(s => s.TrueNextStepId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<JourneyStep>()
            .HasOne(s => s.FalseNextStep)
            .WithMany()
            .HasForeignKey(s => s.FalseNextStepId)
            .OnDelete(DeleteBehavior.NoAction);

        // JourneyEnrollment
        modelBuilder.Entity<JourneyEnrollment>()
            .HasOne(e => e.Journey)
            .WithMany(j => j.Enrollments)
            .HasForeignKey(e => e.JourneyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JourneyEnrollment>()
            .HasOne(e => e.Contact)
            .WithMany()
            .HasForeignKey(e => e.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JourneyEnrollment>()
            .HasOne(e => e.CurrentStep)
            .WithMany()
            .HasForeignKey(e => e.CurrentStepId)
            .OnDelete(DeleteBehavior.NoAction);

        // JourneyStepExecution
        modelBuilder.Entity<JourneyStepExecution>()
            .HasOne(x => x.Enrollment)
            .WithMany(e => e.StepExecutions)
            .HasForeignKey(x => x.EnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JourneyStepExecution>()
            .HasOne(x => x.Step)
            .WithMany()
            .HasForeignKey(x => x.StepId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
