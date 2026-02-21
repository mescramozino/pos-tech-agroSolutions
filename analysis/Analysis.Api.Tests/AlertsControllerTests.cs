using Microsoft.EntityFrameworkCore;
using Analysis.Api.Controllers;
using Analysis.Api.Data;
using Analysis.Api.Entities;
using Xunit;

namespace Analysis.Api.Tests;

public class AlertsControllerTests
{
    private static AnalysisDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AnalysisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AnalysisDbContext(options);
    }

    [Fact]
    public async Task GetAlerts_EmptyDb_ReturnsEmptyList()
    {
        await using var db = CreateDb();
        db.Database.EnsureCreated();
        var controller = new AlertsController(db);

        var result = await controller.GetAlerts(null, null, default);

        var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<AlertDto>>(ok.Value);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetAlerts_WithAlerts_ReturnsAll()
    {
        await using var db = CreateDb();
        db.Database.EnsureCreated();
        var plotId = Guid.NewGuid();
        var alert1 = new Alert
        {
            Id = Guid.NewGuid(),
            PlotId = plotId,
            Type = "Drought",
            Message = "Alerta de seca",
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };
        var alert2 = new Alert
        {
            Id = Guid.NewGuid(),
            PlotId = plotId,
            Type = "Plague",
            Message = "Risco de praga",
            CreatedAt = DateTime.UtcNow
        };
        db.Alerts.AddRange(alert1, alert2);
        await db.SaveChangesAsync();
        var controller = new AlertsController(db);

        var result = await controller.GetAlerts(null, null, default);

        var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<AlertDto>>(ok.Value).ToList();
        Assert.Equal(2, list.Count);
        Assert.Equal(alert2.Id, list[0].Id);
        Assert.Equal("Plague", list[0].Type);
        Assert.Equal(alert1.Id, list[1].Id);
    }

    [Fact]
    public async Task GetAlerts_FilterByPlotId_ReturnsOnlyThatPlot()
    {
        await using var db = CreateDb();
        db.Database.EnsureCreated();
        var plotA = Guid.NewGuid();
        var plotB = Guid.NewGuid();
        db.Alerts.Add(new Alert { Id = Guid.NewGuid(), PlotId = plotA, Type = "Drought", Message = "A", CreatedAt = DateTime.UtcNow });
        db.Alerts.Add(new Alert { Id = Guid.NewGuid(), PlotId = plotB, Type = "Info", Message = "B", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var controller = new AlertsController(db);

        var result = await controller.GetAlerts(plotA, null, default);

        var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<AlertDto>>(ok.Value).ToList();
        Assert.Single(list);
        Assert.Equal(plotA, list[0].PlotId);
        Assert.Equal("Drought", list[0].Type);
    }
}
