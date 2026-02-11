namespace Ingestion.Application;

public record SensorIngestionRequest(Guid PlotId, List<SensorReadingDto> Readings);

public record SensorReadingDto(string Type, double Value, DateTime Timestamp);
