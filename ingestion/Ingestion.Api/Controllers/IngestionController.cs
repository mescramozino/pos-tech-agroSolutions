using Microsoft.AspNetCore.Mvc;
using Ingestion.Application;

namespace Ingestion.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IngestionController : ControllerBase
{
    private static readonly HashSet<string> ValidTypes = new(StringComparer.OrdinalIgnoreCase)
        { "moisture", "umidade", "temperature", "temperatura", "precipitation", "precipitacao" };

    private readonly ISensorReadingsStore _store;
    private readonly ISensorReadingsPublisher _publisher;

    public IngestionController(ISensorReadingsStore store, ISensorReadingsPublisher publisher)
    {
        _store = store;
        _publisher = publisher;
    }

    [HttpPost("sensors")]
    public async Task<IActionResult> PostSensors([FromBody] SensorIngestionRequest? request, CancellationToken ct)
    {
        if (request == null || request.PlotId == Guid.Empty)
            return BadRequest("PlotId is required.");

        if (request.Readings == null || request.Readings.Count == 0)
            return BadRequest("At least one reading is required.");

        foreach (var r in request.Readings)
        {
            var type = (r.Type ?? "").Trim();
            if (string.IsNullOrEmpty(type) || !ValidTypes.Contains(type))
                return BadRequest($"Invalid reading type: '{r.Type}'. Valid: moisture, temperature, precipitation.");
        }

        foreach (var r in request.Readings)
        {
            var type = r.Type!.Trim();
            _store.Add(request.PlotId, type, r.Value, r.Timestamp);
            await _publisher.PublishAsync(request.PlotId, type, r.Value, r.Timestamp, ct);
        }

        return Accepted();
    }
}
