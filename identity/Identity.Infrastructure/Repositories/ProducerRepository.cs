using Microsoft.EntityFrameworkCore;
using Identity.Application.Interfaces;
using Identity.Domain;
using Identity.Infrastructure.Data;

namespace Identity.Infrastructure.Repositories;

public class ProducerRepository : IProducerRepository
{
    private readonly IdentityDbContext _db;

    public ProducerRepository(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<Producer?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return await _db.Producers
            .FirstOrDefaultAsync(p => p.Email == email, ct);
    }

    public async Task<Producer?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Producers.FindAsync([id], ct);
    }

    public async Task<IReadOnlyList<Producer>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Producers
            .OrderBy(p => p.Email)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Producer producer, CancellationToken ct = default)
    {
        _db.Producers.Add(producer);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Producer producer, CancellationToken ct = default)
    {
        _db.Producers.Update(producer);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var producer = await _db.Producers.FindAsync([id], ct);
        if (producer != null)
        {
            _db.Producers.Remove(producer);
            await _db.SaveChangesAsync(ct);
        }
    }
}
