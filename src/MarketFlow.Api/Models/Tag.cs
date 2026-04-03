namespace MarketFlow.Api.Models;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<ContactTag> Contacts { get; set; } = new List<ContactTag>();
}
