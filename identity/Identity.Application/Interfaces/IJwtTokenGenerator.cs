namespace Identity.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string Generate(string producerId, string email);
}
