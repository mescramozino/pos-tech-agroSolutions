using Microsoft.EntityFrameworkCore;
using Properties.Application;
using Properties.Domain;

namespace Properties.Infrastructure;

public class PlotRepository : IPlotRepository
{
    private readonly PropertiesDbContext _db;

    public PlotRepository(PropertiesDbContext db)
    {
        _db = db;
    }

    public async Task<Plot?> GetByIdAsync(Guid id, Guid producerId, CancellationToken ct = default)
    {
        return await _db.Plots
            .Include(p => p.Property)
            .FirstOrDefaultAsync(p => p.Id == id && p.Property != null && p.Property.ProducerId == producerId, ct);
    }

    public async Task<IReadOnlyList<Plot>> GetByPropertyIdAsync(Guid propertyId, CancellationToken ct = default)
    {
        return await _db.Plots
            .Where(pl => pl.PropertyId == propertyId)
            .ToListAsync(ct);
    }

    public async Task<Plot> AddAsync(Plot plot, CancellationToken ct = default)
    {
        _db.Plots.Add(plot);
        await _db.SaveChangesAsync(ct);
        return plot;
    }

    public async Task UpdateAsync(Plot plot, CancellationToken ct = default)
    {
        _db.Plots.Update(plot);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Plot plot, CancellationToken ct = default)
    {
        _db.Plots.Remove(plot);
        await _db.SaveChangesAsync(ct);
    }
}
