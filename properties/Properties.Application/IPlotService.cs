namespace Properties.Application;

public interface IPlotService
{
    Task<PlotResponse?> GetByIdAsync(Guid id, Guid? producerId, CancellationToken ct = default);
    Task<bool> UpdateAsync(Guid id, UpdatePlotRequest request, Guid? producerId, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, Guid? producerId, CancellationToken ct = default);
}
