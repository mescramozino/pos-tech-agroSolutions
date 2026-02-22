using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Analysis.Api.Data;

namespace Analysis.Api.Controllers;

[ApiController]
[Route("api/analysis/plots/{plotId:guid}/status")]
public class StatusController : ControllerBase
{
    private readonly AnalysisDbContext _db;

    public StatusController(AnalysisDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<PlotStatusDto>> GetStatus(Guid plotId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var windowStart = now.AddHours(-24);
        var recentDrought = await _db.Alerts
            .AnyAsync(a => a.PlotId == plotId && a.Type == "Drought" && a.CreatedAt >= windowStart, ct);
        if (recentDrought)
        {
            return Ok(new PlotStatusDto(plotId, "DroughtAlert", "Alerta de Seca: umidade abaixo de 30% por mais de 24h."));
        }
        var recentPlague = await _db.Alerts
            .AnyAsync(a => a.PlotId == plotId && a.Type == "Plague" && a.CreatedAt >= windowStart, ct);
        if (recentPlague)
        {
            return Ok(new PlotStatusDto(plotId, "PlagueRisk", "Risco de Praga: umidade e temperatura elevadas favorecem pragas e fungos."));
        }
        var recentFrost = await _db.Alerts
            .AnyAsync(a => a.PlotId == plotId && a.Type == "Frost" && a.CreatedAt >= windowStart, ct);
        if (recentFrost)
        {
            return Ok(new PlotStatusDto(plotId, "FrostRisk", "Alerta de Geada: temperatura mínima abaixo de 2°C nas últimas 24h."));
        }
        var recentFlood = await _db.Alerts
            .AnyAsync(a => a.PlotId == plotId && a.Type == "Flood" && a.CreatedAt >= windowStart, ct);
        if (recentFlood)
        {
            return Ok(new PlotStatusDto(plotId, "FloodRisk", "Risco de Alagamento: umidade do solo acima de 90% nas últimas 24h."));
        }
        return Ok(new PlotStatusDto(plotId, "Normal", "Sem alertas."));
    }
}

public record PlotStatusDto(Guid PlotId, string Status, string Message);
