namespace Analysis.Api.Entities;

public class SensorReading
{
    public Guid Id { get; set; }
    public Guid PlotId { get; set; }
    public string Type { get; set; } = string.Empty;
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
    public DateTime IngestedAt { get; set; }
}
