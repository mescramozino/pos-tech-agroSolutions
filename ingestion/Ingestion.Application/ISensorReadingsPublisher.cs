namespace Ingestion.Application;

public interface ISensorReadingsPublisher
{
    Task PublishAsync(Guid plotId, string type, double value, DateTime timestamp, CancellationToken ct = default);
}
