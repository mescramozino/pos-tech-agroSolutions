using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Analysis.Api.Data;

namespace Analysis.Api.Controllers;

[ApiController]
[Route("api/analysis/alerts")]
public class AlertsController : ControllerBase
{
    private readonly AnalysisDbContext _db;

    public AlertsController(AnalysisDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AlertDto>>> GetAlerts(
        [FromQuery] Guid? plotId,
        [FromQuery] DateTime? from,
        CancellationToken ct)
    {
        var q = _db.Alerts.AsQueryable();
        if (plotId.HasValue) q = q.Where(a => a.PlotId == plotId.Value);
        if (from.HasValue) q = q.Where(a => a.CreatedAt >= from.Value);
        var list = await q.OrderByDescending(a => a.CreatedAt)
            .Select(a => new AlertDto(a.Id, a.PlotId, a.Type, a.Message, a.CreatedAt))
            .ToListAsync(ct);
        return Ok(list);
    }
}

public record AlertDto(Guid Id, Guid PlotId, string Type, string Message, DateTime CreatedAt);
