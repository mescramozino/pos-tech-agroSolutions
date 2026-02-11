namespace Ingestion.Application;

public interface ISensorReadingsStore
{
    void Add(Guid plotId, string type, double value, DateTime timestamp);
}
