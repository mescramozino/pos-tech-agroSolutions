using Ingestion.Application;

namespace Ingestion.Infrastructure;

internal class StoredReading
{
    public Guid Id { get; set; }
    public Guid PlotId { get; set; }
    public string Type { get; set; } = string.Empty;
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
}

public class SensorReadingsStore : ISensorReadingsStore
{
    private static readonly List<StoredReading> _readings = new();
    private static readonly object _lock = new();

    public void Add(Guid plotId, string type, double value, DateTime timestamp)
    {
        lock (_lock)
        {
            _readings.Add(new StoredReading
            {
                Id = Guid.NewGuid(),
                PlotId = plotId,
                Type = type,
                Value = value,
                Timestamp = timestamp
            });
        }
    }
}
