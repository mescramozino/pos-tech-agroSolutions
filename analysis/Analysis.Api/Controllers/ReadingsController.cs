using Microsoft.AspNetCore.Mvc;
using Analysis.Api.Services;

namespace Analysis.Api.Controllers;

[ApiController]
[Route("api/analysis/plots/{plotId:guid}/readings")]
public class ReadingsController : ControllerBase
{
    private readonly ISensorReadingsTimeSeriesStore _store;

    public ReadingsController(ISensorReadingsTimeSeriesStore store)
    {
        _store = store;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReadingDto>>> GetReadings(
        Guid plotId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? type,
        CancellationToken ct)
    {
        var list = await _store.GetReadingsAsync(plotId, from, to, type, ct);
        var dtos = list.Select((r, i) => new ReadingDto(
            ReadingIdHelper.CreateDeterministicId(r.PlotId, r.Type, r.Timestamp, i),
            r.PlotId,
            r.Type,
            r.Value,
            r.Timestamp)).ToList();
        return Ok(dtos);
    }
}

public record ReadingDto(Guid Id, Guid PlotId, string Type, double Value, DateTime Timestamp);

internal static class ReadingIdHelper
{
    public static Guid CreateDeterministicId(Guid plotId, string type, DateTime timestamp, int index)
    {
        var bytes = new byte[16];
        plotId.ToByteArray().CopyTo(bytes, 0);
        var ts = BitConverter.GetBytes(timestamp.Ticks);
        if (ts.Length >= 4) Array.Copy(ts, 0, bytes, 8, 4);
        bytes[14] = (byte)(index >> 8);
        bytes[15] = (byte)index;
        return new Guid(bytes);
    }
}
