using Microsoft.EntityFrameworkCore;
using Properties.Application;
using Properties.Domain;

namespace Properties.Infrastructure;

public class PropertyRepository : IPropertyRepository
{
    private readonly PropertiesDbContext _db;

    public PropertyRepository(PropertiesDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Property>> GetByProducerIdAsync(Guid producerId, CancellationToken ct = default)
    {
        return await _db.Properties
            .Where(p => p.ProducerId == producerId)
            .ToListAsync(ct);
    }

    public async Task<Property?> GetByIdAsync(Guid id, Guid producerId, CancellationToken ct = default)
    {
        return await _db.Properties
            .FirstOrDefaultAsync(p => p.Id == id && p.ProducerId == producerId, ct);
    }

    public async Task<Property> AddAsync(Property property, CancellationToken ct = default)
    {
        _db.Properties.Add(property);
        await _db.SaveChangesAsync(ct);
        return property;
    }

    public async Task UpdateAsync(Property property, CancellationToken ct = default)
    {
        _db.Properties.Update(property);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Property property, CancellationToken ct = default)
    {
        _db.Properties.Remove(property);
        await _db.SaveChangesAsync(ct);
    }
}
