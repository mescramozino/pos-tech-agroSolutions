using Properties.Domain;

namespace Properties.Application;

public interface IPlotRepository
{
    Task<Plot?> GetByIdAsync(Guid id, Guid producerId, CancellationToken ct = default);
    Task<IReadOnlyList<Plot>> GetByPropertyIdAsync(Guid propertyId, CancellationToken ct = default);
    Task<Plot> AddAsync(Plot plot, CancellationToken ct = default);
    Task UpdateAsync(Plot plot, CancellationToken ct = default);
    Task DeleteAsync(Plot plot, CancellationToken ct = default);
}
