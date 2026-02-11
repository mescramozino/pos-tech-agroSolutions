namespace Analysis.Api.Services;

/// <summary>
/// No-op implementation when InfluxDB is not configured; readings API returns empty list.
/// </summary>
public class NullSensorReadingsTimeSeriesStore : ISensorReadingsTimeSeriesStore
{
    public Task WriteAsync(Guid plotId, string type, double value, DateTime timestamp, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<SensorReadingPoint>> GetReadingsAsync(Guid plotId, DateTime? from, DateTime? to, string? type, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<SensorReadingPoint>>(Array.Empty<SensorReadingPoint>());
}
