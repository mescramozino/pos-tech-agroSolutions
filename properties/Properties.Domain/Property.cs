namespace Properties.Domain;

public class Property
{
    public Guid Id { get; set; }
    public Guid ProducerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Plot> Plots { get; set; } = new List<Plot>();
}
