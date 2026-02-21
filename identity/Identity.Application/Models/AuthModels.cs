namespace Identity.Application.Models;

public record RegisterRequest(string Email, string Password);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string Token, string Email);

public record ProducerDto(Guid Id, string Email, DateTime CreatedAt);

public record CreateProducerRequest(string Email, string Password);

public record UpdateProducerRequest(string Email);
