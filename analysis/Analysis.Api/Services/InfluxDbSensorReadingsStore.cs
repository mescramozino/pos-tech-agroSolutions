using System.Text;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

namespace Analysis.Api.Services;

public class InfluxDbSensorReadingsStore : ISensorReadingsTimeSeriesStore
{
    private readonly string _url;
    private readonly string _token;
    private readonly string _org;
    private readonly string _bucket;
    private readonly ILogger<InfluxDbSensorReadingsStore> _logger;

    public InfluxDbSensorReadingsStore(IConfiguration configuration, ILogger<InfluxDbSensorReadingsStore> logger)
    {
        _url = configuration["InfluxDB:Url"] ?? "http://localhost:8086";
        _token = configuration["InfluxDB:Token"] ?? "";
        _org = configuration["InfluxDB:Org"] ?? "agro";
        _bucket = configuration["InfluxDB:Bucket"] ?? "sensor_readings";
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_url) && !string.IsNullOrWhiteSpace(_token);

    public Task WriteAsync(Guid plotId, string type, double value, DateTime timestamp, CancellationToken ct = default)
    {
        if (!IsConfigured) return Task.CompletedTask;
        try
        {
            using var client = new InfluxDBClient(_url, _token);
            var point = PointData.Measurement("sensor_reading")
                .Tag("plotId", plotId.ToString())
                .Tag("type", type)
                .Field("value", value)
                .Timestamp(timestamp, WritePrecision.S);
            using var writeApi = client.GetWriteApi();
            writeApi.WritePoint(point, _bucket, _org);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "InfluxDB write failed for plotId={PlotId}, type={Type}", plotId, type);
        }
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<SensorReadingPoint>> GetReadingsAsync(Guid plotId, DateTime? from, DateTime? to, string? type, CancellationToken ct = default)
    {
        if (!IsConfigured) return Array.Empty<SensorReadingPoint>();
        try
        {
            var fromStr = (from ?? DateTime.UtcNow.AddDays(-30)).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            var toStr = (to ?? DateTime.UtcNow).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            var flux = new StringBuilder();
            flux.Append($@"from(bucket: ""{_bucket}"")
  |> range(start: time(v: ""{fromStr}""), stop: time(v: ""{toStr}""))
  |> filter(fn: (r) => r[""_measurement""] == ""sensor_reading"" and r[""plotId""] == ""{plotId}""");
            if (!string.IsNullOrWhiteSpace(type))
                flux.Append($@" and r[""type""] == ""{type}""");
            flux.Append(@")
  |> sort(columns: [""_time""])");

            using var client = new InfluxDBClient(_url, _token);
            var queryApi = client.GetQueryApi();
            var tables = await queryApi.QueryAsync(flux.ToString(), _org, ct);
            var list = new List<SensorReadingPoint>();
            foreach (var table in tables)
            {
                foreach (var record in table.Records)
                {
                    var ts = record.GetTime().HasValue ? record.GetTime()!.Value.ToDateTimeUtc() : DateTime.UtcNow;
                    var val = record.GetValue();
                    var value = val is double d ? d : Convert.ToDouble(val);
                    var plotIdStr = record.GetValueByKey("plotId")?.ToString() ?? plotId.ToString();
                    var typeStr = record.GetValueByKey("type")?.ToString() ?? "moisture";
                    list.Add(new SensorReadingPoint(Guid.Parse(plotIdStr), typeStr, value, ts));
                }
            }
            return list;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "InfluxDB query failed for plotId={PlotId}", plotId);
            return Array.Empty<SensorReadingPoint>();
        }
    }
}
