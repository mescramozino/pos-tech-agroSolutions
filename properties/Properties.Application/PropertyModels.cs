namespace Properties.Application;

public record CreatePropertyRequest(string Name, string? Location);

public record UpdatePropertyRequest(string Name, string? Location);

public record PropertyResponse(Guid Id, Guid ProducerId, string Name, string? Location, DateTime CreatedAt);

public record CreatePlotRequest(string Name, string Culture);

public record UpdatePlotRequest(string Name, string Culture);

public record PlotResponse(Guid Id, Guid PropertyId, string Name, string Culture, DateTime CreatedAt);
