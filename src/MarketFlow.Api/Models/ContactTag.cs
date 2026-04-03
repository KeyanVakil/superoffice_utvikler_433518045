namespace MarketFlow.Api.Models;

public class ContactTag
{
    public int ContactId { get; set; }
    public Contact Contact { get; set; } = null!;

    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
