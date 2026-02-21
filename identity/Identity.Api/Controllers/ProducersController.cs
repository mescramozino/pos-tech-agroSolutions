using Microsoft.AspNetCore.Mvc;
using Identity.Application.Interfaces;
using Identity.Application.Models;
using Identity.Domain;

namespace Identity.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProducersController : ControllerBase
{
    private readonly IProducerRepository _producerRepository;

    public ProducersController(IProducerRepository producerRepository)
    {
        _producerRepository = producerRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProducerDto>>> List(CancellationToken ct)
    {
        var list = await _producerRepository.GetAllAsync(ct);
        var dtos = list.Select(p => new ProducerDto(p.Id, p.Email, p.CreatedAt)).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProducerDto>> GetById(Guid id, CancellationToken ct)
    {
        var producer = await _producerRepository.GetByIdAsync(id, ct);
        if (producer == null)
            return NotFound();
        return Ok(new ProducerDto(producer.Id, producer.Email, producer.CreatedAt));
    }

    [HttpPost]
    public async Task<ActionResult<ProducerDto>> Create([FromBody] CreateProducerRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Email and password are required.");

        var existing = await _producerRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);
        if (existing != null)
            return BadRequest("Email already registered.");

        var producer = new Producer
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };
        await _producerRepository.AddAsync(producer, ct);
        return Ok(new ProducerDto(producer.Id, producer.Email, producer.CreatedAt));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProducerRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("Email is required.");

        var producer = await _producerRepository.GetByIdAsync(id, ct);
        if (producer == null)
            return NotFound();

        var newEmail = request.Email.Trim().ToLowerInvariant();
        if (newEmail != producer.Email)
        {
            var existing = await _producerRepository.GetByEmailAsync(newEmail, ct);
            if (existing != null)
                return BadRequest("Email already in use.");
            producer.Email = newEmail;
        }
        await _producerRepository.UpdateAsync(producer, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var producer = await _producerRepository.GetByIdAsync(id, ct);
        if (producer == null)
            return NotFound();
        await _producerRepository.DeleteAsync(id, ct);
        return NoContent();
    }
}
