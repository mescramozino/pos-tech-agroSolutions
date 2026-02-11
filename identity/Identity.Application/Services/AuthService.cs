using Identity.Application.Interfaces;
using Identity.Application.Models;
using Identity.Domain;

namespace Identity.Application.Services;

public class AuthService : IAuthService
{
    private readonly IProducerRepository _producerRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(IProducerRepository producerRepository, IJwtTokenGenerator jwtTokenGenerator)
    {
        _producerRepository = producerRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return null;

        var existing = await _producerRepository.GetByEmailAsync(request.Email.Trim(), ct);
        if (existing != null)
            return null;

        var producer = new Producer
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };
        await _producerRepository.AddAsync(producer, ct);

        var token = _jwtTokenGenerator.Generate(producer.Id.ToString(), producer.Email);
        return new AuthResponse(token, producer.Email);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var producer = await _producerRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);
        if (producer == null)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(request.Password, producer.PasswordHash))
            return null;

        var token = _jwtTokenGenerator.Generate(producer.Id.ToString(), producer.Email);
        return new AuthResponse(token, producer.Email);
    }
}
