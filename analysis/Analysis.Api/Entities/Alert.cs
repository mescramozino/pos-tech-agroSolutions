namespace Analysis.Api.Entities;

public class Alert
{
    public Guid Id { get; set; }
    public Guid PlotId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
