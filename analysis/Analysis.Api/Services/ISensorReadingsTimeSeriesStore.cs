namespace Analysis.Api.Services;

public interface ISensorReadingsTimeSeriesStore
{
    Task WriteAsync(Guid plotId, string type, double value, DateTime timestamp, CancellationToken ct = default);
    Task<IReadOnlyList<SensorReadingPoint>> GetReadingsAsync(Guid plotId, DateTime? from, DateTime? to, string? type, CancellationToken ct = default);
}

public record SensorReadingPoint(Guid PlotId, string Type, double Value, DateTime Timestamp);
