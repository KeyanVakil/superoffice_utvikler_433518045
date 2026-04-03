using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using MarketFlow.Api.Data;
using MarketFlow.Api.Dtos;
using MarketFlow.Api.Models;

namespace MarketFlow.Api.Services;

public class ContactService
{
    private readonly AppDbContext _db;

    public ContactService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ContactListDto>> GetContactsAsync(
        int page = 1, int pageSize = 25, string? search = null,
        string? industry = null, string? tag = null,
        string sortBy = "CreatedAt", string sortDir = "desc")
    {
        var query = _db.Contacts
            .Include(c => c.Tags).ThenInclude(ct => ct.Tag)
            .Include(c => c.ActivityEvents)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(c =>
                c.FirstName.ToLower().Contains(s) ||
                c.LastName.ToLower().Contains(s) ||
                c.Email.ToLower().Contains(s) ||
                (c.Company != null && c.Company.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(industry))
            query = query.Where(c => c.Industry == industry);

        if (!string.IsNullOrWhiteSpace(tag))
            query = query.Where(c => c.Tags.Any(ct => ct.Tag.Name == tag));

        var totalCount = await query.CountAsync();

        query = sortBy.ToLower() switch
        {
            "firstname" => sortDir == "asc" ? query.OrderBy(c => c.FirstName) : query.OrderByDescending(c => c.FirstName),
            "lastname" => sortDir == "asc" ? query.OrderBy(c => c.LastName) : query.OrderByDescending(c => c.LastName),
            "email" => sortDir == "asc" ? query.OrderBy(c => c.Email) : query.OrderByDescending(c => c.Email),
            "company" => sortDir == "asc" ? query.OrderBy(c => c.Company) : query.OrderByDescending(c => c.Company),
            "industry" => sortDir == "asc" ? query.OrderBy(c => c.Industry) : query.OrderByDescending(c => c.Industry),
            _ => sortDir == "asc" ? query.OrderBy(c => c.CreatedAt) : query.OrderByDescending(c => c.CreatedAt)
        };

        var contacts = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var cutoff = DateTime.UtcNow.AddDays(-90);
        var items = contacts.Select(c => MapToListDto(c, cutoff)).ToList();

        return new PagedResult<ContactListDto>(items, totalCount, page, pageSize);
    }

    public async Task<ContactDetailDto?> GetContactAsync(int id)
    {
        var contact = await _db.Contacts
            .Include(c => c.Tags).ThenInclude(ct => ct.Tag)
            .Include(c => c.ActivityEvents).ThenInclude(e => e.Campaign)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contact is null) return null;

        var cutoff = DateTime.UtcNow.AddDays(-90);
        var score = ComputeEngagementScore(contact.ActivityEvents, cutoff);
        var lastActivity = contact.ActivityEvents.OrderByDescending(e => e.OccurredAt).FirstOrDefault()?.OccurredAt;

        var timeline = contact.ActivityEvents
            .OrderByDescending(e => e.OccurredAt)
            .Select(e => new ActivityEventDto(e.Id, e.EventType, e.OccurredAt, e.CampaignId, e.Campaign?.Name, e.Metadata))
            .ToList();

        return new ContactDetailDto(
            contact.Id, contact.FirstName, contact.LastName, contact.Email,
            contact.Company, contact.Industry,
            contact.Tags.Select(ct => ct.Tag.Name).ToArray(),
            score, lastActivity, contact.CreatedAt, contact.UpdatedAt, timeline);
    }

    public async Task<ContactDetailDto> CreateContactAsync(CreateContactDto dto)
    {
        var contact = new Contact
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Company = dto.Company,
            Industry = dto.Industry,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Contacts.Add(contact);
        await _db.SaveChangesAsync();

        if (dto.Tags?.Length > 0)
        {
            await AssignTagsAsync(contact.Id, dto.Tags);
        }

        return (await GetContactAsync(contact.Id))!;
    }

    public async Task<ContactDetailDto?> UpdateContactAsync(int id, UpdateContactDto dto)
    {
        var contact = await _db.Contacts.Include(c => c.Tags).FirstOrDefaultAsync(c => c.Id == id);
        if (contact is null) return null;

        contact.FirstName = dto.FirstName;
        contact.LastName = dto.LastName;
        contact.Email = dto.Email;
        contact.Company = dto.Company;
        contact.Industry = dto.Industry;
        contact.UpdatedAt = DateTime.UtcNow;

        // Replace tags
        _db.ContactTags.RemoveRange(contact.Tags);
        await _db.SaveChangesAsync();

        if (dto.Tags?.Length > 0)
        {
            await AssignTagsAsync(contact.Id, dto.Tags);
        }

        return (await GetContactAsync(id))!;
    }

    public async Task<bool> DeleteContactAsync(int id)
    {
        var contact = await _db.Contacts.FindAsync(id);
        if (contact is null) return false;

        _db.Contacts.Remove(contact);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<ContactImportResultDto> ImportContactsAsync(Stream csvStream)
    {
        int imported = 0, skipped = 0;
        var errors = new List<string>();

        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null
        });

        var records = csv.GetRecords<CsvContactRecord>().ToList();
        int row = 1;
        foreach (var record in records)
        {
            row++;
            if (string.IsNullOrWhiteSpace(record.Email))
            {
                errors.Add($"Row {row}: Missing email");
                skipped++;
                continue;
            }

            if (await _db.Contacts.AnyAsync(c => c.Email == record.Email))
            {
                skipped++;
                continue;
            }

            var contact = new Contact
            {
                FirstName = record.FirstName ?? string.Empty,
                LastName = record.LastName ?? string.Empty,
                Email = record.Email,
                Company = record.Company,
                Industry = record.Industry,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Contacts.Add(contact);
            await _db.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(record.Tags))
            {
                var tagNames = record.Tags.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                await AssignTagsAsync(contact.Id, tagNames);
            }

            imported++;
        }

        return new ContactImportResultDto(imported, skipped, errors);
    }

    private async Task AssignTagsAsync(int contactId, string[] tagNames)
    {
        foreach (var tagName in tagNames)
        {
            var normalizedName = tagName.Trim().ToLower();
            var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Name == normalizedName);
            if (tag is null)
            {
                tag = new Tag { Name = normalizedName };
                _db.Tags.Add(tag);
                await _db.SaveChangesAsync();
            }

            if (!await _db.ContactTags.AnyAsync(ct => ct.ContactId == contactId && ct.TagId == tag.Id))
            {
                _db.ContactTags.Add(new ContactTag { ContactId = contactId, TagId = tag.Id });
            }
        }
        await _db.SaveChangesAsync();
    }

    private static ContactListDto MapToListDto(Contact c, DateTime cutoff)
    {
        var score = ComputeEngagementScore(c.ActivityEvents, cutoff);
        var lastActivity = c.ActivityEvents.OrderByDescending(e => e.OccurredAt).FirstOrDefault()?.OccurredAt;
        return new ContactListDto(
            c.Id, c.FirstName, c.LastName, c.Email, c.Company, c.Industry,
            c.Tags.Select(ct => ct.Tag.Name).ToArray(), score, lastActivity);
    }

    private static double ComputeEngagementScore(IEnumerable<ActivityEvent> events, DateTime cutoff)
    {
        var recentEvents = events.Where(e => e.OccurredAt >= cutoff).ToList();
        int rawScore = recentEvents.Count(e => e.EventType == "open") * 2
                     + recentEvents.Count(e => e.EventType == "click") * 3;
        return Math.Min(rawScore, 100);
    }

    private class CsvContactRecord
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Company { get; set; }
        public string? Industry { get; set; }
        public string? Tags { get; set; }
    }
}
