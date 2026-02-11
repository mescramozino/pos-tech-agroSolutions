namespace Properties.Domain;

public class Plot
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Culture { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Property? Property { get; set; }
}
