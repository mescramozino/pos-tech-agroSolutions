using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Properties.Api.Services;
using Properties.Application;

namespace Properties.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AllowJwtOrProducerId")]
public class PropertiesController : ControllerBase
{
    private readonly IPropertyService _propertyService;
    private readonly IProducerIdAccessor _producerIdAccessor;

    public PropertiesController(IPropertyService propertyService, IProducerIdAccessor producerIdAccessor)
    {
        _propertyService = propertyService;
        _producerIdAccessor = producerIdAccessor;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PropertyResponse>>> GetAll(CancellationToken ct)
    {
        var producerId = _producerIdAccessor.GetProducerId();
        if (producerId == null) return Unauthorized();

        var list = await _propertyService.GetByProducerIdAsync(producerId, ct);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PropertyResponse>> GetById(Guid id, CancellationToken ct)
    {
        var producerId = _producerIdAccessor.GetProducerId();
        if (producerId == null) return Unauthorized();

        var property = await _propertyService.GetByIdAsync(id, producerId, ct);
        if (property == null) return NotFound();
        return Ok(property);
    }

    [HttpPost]
    public async Task<ActionResult<PropertyResponse>> Create([FromBody] CreatePropertyRequest request, CancellationToken ct)
    {
        var producerId = _producerIdAccessor.GetProducerId();
        if (producerId == null) return Unauthorized();

        var property = await _propertyService.CreateAsync(request, producerId, ct);
        if (property == null) return Unauthorized();
        return CreatedAtAction(nameof(GetById), new { id = property.Id }, property);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePropertyRequest request, CancellationToken ct)
    {
        var producerId = _producerIdAccessor.GetProducerId();
        if (producerId == null) return Unauthorized();

        var updated = await _propertyService.UpdateAsync(id, request, producerId, ct);
        if (!updated) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var producerId = _producerIdAccessor.GetProducerId();
        if (producerId == null) return Unauthorized();

        var deleted = await _propertyService.DeleteAsync(id, producerId, ct);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpGet("{propertyId:guid}/plots")]
    public async Task<ActionResult<IEnumerable<PlotResponse>>> GetPlots(Guid propertyId, CancellationToken ct)
    {
        var producerId = _producerIdAccessor.GetProducerId();
        if (producerId == null) return Unauthorized();

        var list = await _propertyService.GetPlotsAsync(propertyId, producerId, ct);
        if (list == null) return NotFound();
        return Ok(list);
    }

    [HttpPost("{propertyId:guid}/plots")]
    public async Task<ActionResult<PlotResponse>> CreatePlot(Guid propertyId, [FromBody] CreatePlotRequest request, CancellationToken ct)
    {
        var producerId = _producerIdAccessor.GetProducerId();
        if (producerId == null) return Unauthorized();

        var plot = await _propertyService.CreatePlotAsync(propertyId, request, producerId, ct);
        if (plot == null) return NotFound();
        return CreatedAtAction(nameof(PlotsController.GetById), "Plots", new { id = plot.Id }, plot);
    }
}
