using System.Text;
using System.Text.Json;
using Analysis.Api.Data;
using Analysis.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Analysis.Api.Services;

public class SensorReadingsConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SensorReadingsConsumer> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public SensorReadingsConsumer(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<SensorReadingsConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var host = _configuration["RabbitMQ:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogInformation("RabbitMQ:Host not configured; consumer not starting.");
            return;
        }

        var queue = _configuration["RabbitMQ:Queue"] ?? "sensor.readings";
        var factory = new RabbitMQ.Client.ConnectionFactory
        {
            HostName = host,
            Port = int.TryParse(_configuration["RabbitMQ:Port"], out var p) ? p : 5672,
            UserName = _configuration["RabbitMQ:User"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest"
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();
                channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false, arguments: null);
                var consumer = new RabbitMQ.Client.Events.EventingBasicConsumer(channel);
                consumer.Received += (_, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var json = Encoding.UTF8.GetString(body);
                        var msg = JsonSerializer.Deserialize<SensorReadingMessage>(json, JsonOptions);
                        if (msg == null) return;
                        using var scope = _serviceProvider.CreateScope();
                        var store = scope.ServiceProvider.GetRequiredService<ISensorReadingsTimeSeriesStore>();
                        var db = scope.ServiceProvider.GetRequiredService<AnalysisDbContext>();
                        PersistAndEvaluateAsync(store, db, msg).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing message");
                    }
                };
                channel.BasicConsume(queue, true, "analysis-consumer", false, false, null, consumer);
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RabbitMQ connection failed; retrying in 10s");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private static async Task PersistAndEvaluateAsync(ISensorReadingsTimeSeriesStore store, AnalysisDbContext db, SensorReadingMessage msg)
    {
        var type = msg.Type ?? "moisture";
        await store.WriteAsync(msg.PlotId, type, msg.Value, msg.Timestamp);

        if (string.Equals(type, "moisture", StringComparison.OrdinalIgnoreCase))
            await EvaluateDroughtRuleAsync(store, db, msg.PlotId, msg.Timestamp, msg.Value);

        if (string.Equals(type, "moisture", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(type, "temperature", StringComparison.OrdinalIgnoreCase))
            await EvaluatePlagueRuleAsync(store, db, msg.PlotId, msg.Timestamp);
    }

    private static async Task EvaluateDroughtRuleAsync(ISensorReadingsTimeSeriesStore store, AnalysisDbContext db, Guid plotId, DateTime asOf, double currentMoisture)
    {
        var windowStart = asOf.AddHours(-24);
        var moistureReadings = await store.GetReadingsAsync(plotId, windowStart, asOf, "moisture");
        var values = moistureReadings.Select(r => r.Value).ToList();
        if (currentMoisture < 30) values.Add(currentMoisture);

        if (values.Count == 0) return;
        var allBelow30 = values.All(v => v < 30);
        if (!allBelow30) return;

        var alreadyAlerted = await db.Alerts.AnyAsync(a =>
            a.PlotId == plotId && a.Type == "Drought" && a.CreatedAt >= windowStart);
        if (alreadyAlerted) return;

        db.Alerts.Add(new Alert
        {
            Id = Guid.NewGuid(),
            PlotId = plotId,
            Type = "Drought",
            Message = "Alerta de Seca: umidade abaixo de 30% por mais de 24h.",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Risco de Praga: umidade alta (&gt; 75%) e temperatura alta (&gt; 26 °C) nas últimas 24h
    /// favorecem proliferação de pragas e fungos.
    /// </summary>
    private static async Task EvaluatePlagueRuleAsync(ISensorReadingsTimeSeriesStore store, AnalysisDbContext db, Guid plotId, DateTime asOf)
    {
        var windowStart = asOf.AddHours(-24);
        var moistureReadings = await store.GetReadingsAsync(plotId, windowStart, asOf, "moisture");
        var tempReadings = await store.GetReadingsAsync(plotId, windowStart, asOf, "temperature");
        if (moistureReadings.Count == 0 || tempReadings.Count == 0) return;

        var avgMoisture = moistureReadings.Average(r => r.Value);
        var avgTemp = tempReadings.Average(r => r.Value);
        if (avgMoisture <= 75 || avgTemp <= 26) return;

        var alreadyAlerted = await db.Alerts.AnyAsync(a =>
            a.PlotId == plotId && a.Type == "Plague" && a.CreatedAt >= windowStart);
        if (alreadyAlerted) return;

        db.Alerts.Add(new Alert
        {
            Id = Guid.NewGuid(),
            PlotId = plotId,
            Type = "Plague",
            Message = "Risco de Praga: umidade e temperatura elevadas nas últimas 24h favorecem pragas e fungos.",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private class SensorReadingMessage
    {
        public Guid PlotId { get; set; }
        public string? Type { get; set; }
        public double Value { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime IngestedAt { get; set; }
    }
}
