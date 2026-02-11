using Properties.Domain;

namespace Properties.Application;

public interface IPropertyRepository
{
    Task<IReadOnlyList<Property>> GetByProducerIdAsync(Guid producerId, CancellationToken ct = default);
    Task<Property?> GetByIdAsync(Guid id, Guid producerId, CancellationToken ct = default);
    Task<Property> AddAsync(Property property, CancellationToken ct = default);
    Task UpdateAsync(Property property, CancellationToken ct = default);
    Task DeleteAsync(Property property, CancellationToken ct = default);
}
