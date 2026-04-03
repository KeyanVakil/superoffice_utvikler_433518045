using Microsoft.AspNetCore.Mvc;
using MarketFlow.Api.Dtos;
using MarketFlow.Api.Services;

namespace MarketFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactsController : ControllerBase
{
    private readonly ContactService _service;

    public ContactsController(ContactService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ContactListDto>>> GetContacts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] string? industry = null,
        [FromQuery] string? tag = null,
        [FromQuery] string sortBy = "CreatedAt",
        [FromQuery] string sortDir = "desc")
    {
        var result = await _service.GetContactsAsync(page, pageSize, search, industry, tag, sortBy, sortDir);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ContactDetailDto>> GetContact(int id)
    {
        var contact = await _service.GetContactAsync(id);
        if (contact is null) return NotFound();
        return Ok(contact);
    }

    [HttpPost]
    public async Task<ActionResult<ContactDetailDto>> CreateContact([FromBody] CreateContactDto dto)
    {
        var contact = await _service.CreateContactAsync(dto);
        return CreatedAtAction(nameof(GetContact), new { id = contact.Id }, contact);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ContactDetailDto>> UpdateContact(int id, [FromBody] UpdateContactDto dto)
    {
        var contact = await _service.UpdateContactAsync(id, dto);
        if (contact is null) return NotFound();
        return Ok(contact);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteContact(int id)
    {
        var deleted = await _service.DeleteContactAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPost("import")]
    public async Task<ActionResult<ContactImportResultDto>> ImportContacts(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file uploaded.");

        using var stream = file.OpenReadStream();
        var result = await _service.ImportContactsAsync(stream);
        return Ok(result);
    }
}
