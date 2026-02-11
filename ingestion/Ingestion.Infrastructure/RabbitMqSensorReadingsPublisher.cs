using System.Text;
using System.Text.Json;
using Ingestion.Application;
using Microsoft.Extensions.Configuration;

namespace Ingestion.Infrastructure;

public class RabbitMqSensorReadingsPublisher : ISensorReadingsPublisher
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _user;
    private readonly string _password;
    private readonly string _queue;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public RabbitMqSensorReadingsPublisher(IConfiguration configuration)
    {
        _host = configuration["RabbitMQ:Host"] ?? "localhost";
        _port = int.TryParse(configuration["RabbitMQ:Port"], out var p) ? p : 5672;
        _user = configuration["RabbitMQ:User"] ?? "guest";
        _password = configuration["RabbitMQ:Password"] ?? "guest";
        _queue = configuration["RabbitMQ:Queue"] ?? "sensor.readings";
    }

    public Task PublishAsync(Guid plotId, string type, double value, DateTime timestamp, CancellationToken ct = default)
    {
        var payload = new SensorReadingMessage
        {
            PlotId = plotId,
            Type = type,
            Value = value,
            Timestamp = timestamp,
            IngestedAt = DateTime.UtcNow
        };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var body = Encoding.UTF8.GetBytes(json);

        try
        {
            var factory = new RabbitMQ.Client.ConnectionFactory
            {
                HostName = _host,
                Port = _port,
                UserName = _user,
                Password = _password
            };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            channel.QueueDeclare(_queue, durable: true, exclusive: false, autoDelete: false, arguments: null);
            channel.BasicPublish("", _queue, false, null, body.AsMemory());
        }
        catch
        {
            // Log and ignore for MVP; in production use retry or dead-letter
        }

        return Task.CompletedTask;
    }

    private class SensorReadingMessage
    {
        public Guid PlotId { get; set; }
        public string Type { get; set; } = string.Empty;
        public double Value { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime IngestedAt { get; set; }
    }
}
