using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Properties.Api.Services;
using Properties.Application;

namespace Properties.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AllowJwtOrProducerId")]
public class PlotsController : ControllerBase
{
    private readonly IPlotService _plotService;
    private readonly IProducerIdAccessor _producerIdAccessor;

    public PlotsController(IPlotService plotService, IProducerIdAccessor producerIdAccessor)
    {
        _plotService = plotService;
        _producerIdAccessor = producerIdAccessor;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PlotResponse>> GetById(Guid id, CancellationToken ct)
    {
        var producerId = _producerIdAccessor.GetProducerId();
        if (producerId == null) return Unauthorized();

        var plot = await _plotService.GetByIdAsync(id, producerId, ct);
        if (plot == null) return NotFound();
        return Ok(plot);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePlotRequest request, CancellationToken ct)
    {
        var producerId = _producerIdAccessor.GetProducerId();
        if (producerId == null) return Unauthorized();

        var updated = await _plotService.UpdateAsync(id, request, producerId, ct);
        if (!updated) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var producerId = _producerIdAccessor.GetProducerId();
        if (producerId == null) return Unauthorized();

        var deleted = await _plotService.DeleteAsync(id, producerId, ct);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
