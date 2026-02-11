using Ingestion.Application;

namespace Ingestion.Infrastructure;

public class NullSensorReadingsPublisher : ISensorReadingsPublisher
{
    public Task PublishAsync(Guid plotId, string type, double value, DateTime timestamp, CancellationToken ct = default)
        => Task.CompletedTask;
}
