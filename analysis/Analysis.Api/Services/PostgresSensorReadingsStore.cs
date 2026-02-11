using Analysis.Api.Data;
using Analysis.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Analysis.Api.Services;

public class PostgresSensorReadingsStore : ISensorReadingsTimeSeriesStore
{
    private readonly AnalysisDbContext _db;
    private readonly ILogger<PostgresSensorReadingsStore> _logger;

    public PostgresSensorReadingsStore(AnalysisDbContext db, ILogger<PostgresSensorReadingsStore> logger)
    {
        _db = db;
        _logger = logger;
    }

    public Task WriteAsync(Guid plotId, string type, double value, DateTime timestamp, CancellationToken ct = default)
    {
        var entity = new SensorReading
        {
            Id = Guid.NewGuid(),
            PlotId = plotId,
            Type = type,
            Value = value,
            Timestamp = timestamp.Kind == DateTimeKind.Utc ? timestamp : DateTime.SpecifyKind(timestamp, DateTimeKind.Utc),
            IngestedAt = DateTime.UtcNow
        };
        _db.SensorReadings.Add(entity);
        return _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<SensorReadingPoint>> GetReadingsAsync(Guid plotId, DateTime? from, DateTime? to, string? type, CancellationToken ct = default)
    {
        var fromUtc = (from ?? DateTime.UtcNow.AddDays(-30)).ToUniversalTime();
        var toUtc = (to ?? DateTime.UtcNow).ToUniversalTime();

        var query = _db.SensorReadings
            .Where(r => r.PlotId == plotId && r.Timestamp >= fromUtc && r.Timestamp <= toUtc);

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(r => r.Type == type);

        var list = await query
            .OrderBy(r => r.Timestamp)
            .Select(r => new SensorReadingPoint(r.PlotId, r.Type, r.Value, r.Timestamp))
            .ToListAsync(ct);

        return list;
    }
}
