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

    public async Task AddAsync(Producer producer, CancellationToken ct = default)
    {
        _db.Producers.Add(producer);
        await _db.SaveChangesAsync(ct);
    }
}
