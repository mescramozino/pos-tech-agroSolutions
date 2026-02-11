namespace Properties.Application;

public class PlotService : IPlotService
{
    private readonly IPlotRepository _plotRepository;

    public PlotService(IPlotRepository plotRepository)
    {
        _plotRepository = plotRepository;
    }

    public async Task<PlotResponse?> GetByIdAsync(Guid id, Guid? producerId, CancellationToken ct = default)
    {
        if (producerId == null) return null;
        var plot = await _plotRepository.GetByIdAsync(id, producerId.Value, ct);
        return plot == null ? null : new PlotResponse(plot.Id, plot.PropertyId, plot.Name, plot.Culture, plot.CreatedAt);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdatePlotRequest request, Guid? producerId, CancellationToken ct = default)
    {
        if (producerId == null) return false;
        var plot = await _plotRepository.GetByIdAsync(id, producerId.Value, ct);
        if (plot == null) return false;
        plot.Name = request.Name ?? string.Empty;
        plot.Culture = request.Culture ?? string.Empty;
        await _plotRepository.UpdateAsync(plot, ct);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid? producerId, CancellationToken ct = default)
    {
        if (producerId == null) return false;
        var plot = await _plotRepository.GetByIdAsync(id, producerId.Value, ct);
        if (plot == null) return false;
        await _plotRepository.DeleteAsync(plot, ct);
        return true;
    }
}
