using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Analysis.Api.Data;
using Analysis.Api.Services;
using Xunit;

namespace Analysis.Api.Tests;

public class PostgresSensorReadingsStoreTests
{
    private static AnalysisDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AnalysisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AnalysisDbContext(options);
    }

    [Fact]
    public async Task WriteAsync_AddsReadingToDb()
    {
        await using var db = CreateDb();
        db.Database.EnsureCreated();
        var store = new PostgresSensorReadingsStore(db, NullLogger<PostgresSensorReadingsStore>.Instance);
        var plotId = Guid.NewGuid();
        var ts = DateTime.UtcNow;

        await store.WriteAsync(plotId, "moisture", 45.5, ts);

        var count = await db.SensorReadings.CountAsync();
        Assert.Equal(1, count);
        var r = await db.SensorReadings.FirstAsync();
        Assert.Equal(plotId, r.PlotId);
        Assert.Equal("moisture", r.Type);
        Assert.Equal(45.5, r.Value);
    }

    [Fact]
    public async Task GetReadingsAsync_EmptyDb_ReturnsEmpty()
    {
        await using var db = CreateDb();
        db.Database.EnsureCreated();
        var store = new PostgresSensorReadingsStore(db, NullLogger<PostgresSensorReadingsStore>.Instance);
        var plotId = Guid.NewGuid();
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;

        var list = await store.GetReadingsAsync(plotId, from, to, null, default);

        Assert.Empty(list);
    }

    [Fact]
    public async Task GetReadingsAsync_WithData_ReturnsFilteredByPlotAndType()
    {
        await using var db = CreateDb();
        db.Database.EnsureCreated();
        var store = new PostgresSensorReadingsStore(db, NullLogger<PostgresSensorReadingsStore>.Instance);
        var plotId = Guid.NewGuid();
        var ts = DateTime.UtcNow;
        await store.WriteAsync(plotId, "moisture", 40, ts);
        await store.WriteAsync(plotId, "temperature", 25, ts);
        await store.WriteAsync(Guid.NewGuid(), "moisture", 50, ts);

        var all = await store.GetReadingsAsync(plotId, ts.AddHours(-1), ts.AddHours(1), null, default);
        var moistureOnly = await store.GetReadingsAsync(plotId, ts.AddHours(-1), ts.AddHours(1), "moisture", default);

        Assert.Equal(2, all.Count);
        Assert.Single(moistureOnly);
        Assert.Equal("moisture", moistureOnly[0].Type);
        Assert.Equal(40, moistureOnly[0].Value);
    }
}
