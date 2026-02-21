using Identity.Domain;

namespace Identity.Application.Interfaces;

public interface IProducerRepository
{
    Task<Producer?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Producer?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Producer>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Producer producer, CancellationToken ct = default);
    Task UpdateAsync(Producer producer, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
