namespace Properties.Application;

public interface IPropertyService
{
    Task<IReadOnlyList<PropertyResponse>> GetByProducerIdAsync(Guid? producerId, CancellationToken ct = default);
    Task<PropertyResponse?> GetByIdAsync(Guid id, Guid? producerId, CancellationToken ct = default);
    Task<PropertyResponse?> CreateAsync(CreatePropertyRequest request, Guid? producerId, CancellationToken ct = default);
    Task<bool> UpdateAsync(Guid id, UpdatePropertyRequest request, Guid? producerId, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, Guid? producerId, CancellationToken ct = default);
    Task<IReadOnlyList<PlotResponse>?> GetPlotsAsync(Guid propertyId, Guid? producerId, CancellationToken ct = default);
    Task<PlotResponse?> CreatePlotAsync(Guid propertyId, CreatePlotRequest request, Guid? producerId, CancellationToken ct = default);
}
