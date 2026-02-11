using Properties.Domain;

namespace Properties.Application;

public class PropertyService : IPropertyService
{
    private readonly IPropertyRepository _propertyRepository;
    private readonly IPlotRepository _plotRepository;

    public PropertyService(IPropertyRepository propertyRepository, IPlotRepository plotRepository)
    {
        _propertyRepository = propertyRepository;
        _plotRepository = plotRepository;
    }

    public async Task<IReadOnlyList<PropertyResponse>> GetByProducerIdAsync(Guid? producerId, CancellationToken ct = default)
    {
        if (producerId == null) return Array.Empty<PropertyResponse>();
        var list = await _propertyRepository.GetByProducerIdAsync(producerId.Value, ct);
        return list.Select(p => new PropertyResponse(p.Id, p.ProducerId, p.Name, p.Location, p.CreatedAt)).ToList();
    }

    public async Task<PropertyResponse?> GetByIdAsync(Guid id, Guid? producerId, CancellationToken ct = default)
    {
        if (producerId == null) return null;
        var property = await _propertyRepository.GetByIdAsync(id, producerId.Value, ct);
        return property == null ? null : new PropertyResponse(property.Id, property.ProducerId, property.Name, property.Location, property.CreatedAt);
    }

    public async Task<PropertyResponse?> CreateAsync(CreatePropertyRequest request, Guid? producerId, CancellationToken ct = default)
    {
        if (producerId == null) return null;
        var property = new Domain.Property
        {
            Id = Guid.NewGuid(),
            ProducerId = producerId.Value,
            Name = request.Name ?? string.Empty,
            Location = request.Location,
            CreatedAt = DateTime.UtcNow
        };
        await _propertyRepository.AddAsync(property, ct);
        return new PropertyResponse(property.Id, property.ProducerId, property.Name, property.Location, property.CreatedAt);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdatePropertyRequest request, Guid? producerId, CancellationToken ct = default)
    {
        if (producerId == null) return false;
        var property = await _propertyRepository.GetByIdAsync(id, producerId.Value, ct);
        if (property == null) return false;
        property.Name = request.Name ?? string.Empty;
        property.Location = request.Location;
        await _propertyRepository.UpdateAsync(property, ct);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid? producerId, CancellationToken ct = default)
    {
        if (producerId == null) return false;
        var property = await _propertyRepository.GetByIdAsync(id, producerId.Value, ct);
        if (property == null) return false;
        await _propertyRepository.DeleteAsync(property, ct);
        return true;
    }

    public async Task<IReadOnlyList<PlotResponse>?> GetPlotsAsync(Guid propertyId, Guid? producerId, CancellationToken ct = default)
    {
        if (producerId == null) return null;
        var property = await _propertyRepository.GetByIdAsync(propertyId, producerId.Value, ct);
        if (property == null) return null;
        var list = await _plotRepository.GetByPropertyIdAsync(propertyId, ct);
        return list.Select(pl => new PlotResponse(pl.Id, pl.PropertyId, pl.Name, pl.Culture, pl.CreatedAt)).ToList();
    }

    public async Task<PlotResponse?> CreatePlotAsync(Guid propertyId, CreatePlotRequest request, Guid? producerId, CancellationToken ct = default)
    {
        if (producerId == null) return null;
        var property = await _propertyRepository.GetByIdAsync(propertyId, producerId.Value, ct);
        if (property == null) return null;
        var plot = new Plot
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            Name = request.Name ?? string.Empty,
            Culture = request.Culture ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };
        await _plotRepository.AddAsync(plot, ct);
        return new PlotResponse(plot.Id, plot.PropertyId, plot.Name, plot.Culture, plot.CreatedAt);
    }
}
