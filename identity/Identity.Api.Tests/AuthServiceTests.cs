using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Identity.Application.Interfaces;
using Identity.Application.Models;
using Identity.Application.Services;
using Identity.Infrastructure.Data;
using Identity.Infrastructure.Repositories;
using Identity.Infrastructure.Services;
using Xunit;

namespace Identity.Api.Tests;

public class AuthServiceTests
{
    private static IdentityDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new IdentityDbContext(options);
    }

    private static IConfiguration CreateConfig()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "TestSecretKeyMin32CharactersLong!",
                ["Jwt:Issuer"] = "Identity.Api",
                ["Jwt:Audience"] = "AgroSolutions"
            })
            .Build();
    }

    private static IAuthService CreateAuthService(IdentityDbContext db, IConfiguration config)
    {
        var repo = new ProducerRepository(db);
        var jwtGen = new JwtTokenGenerator(config);
        return new AuthService(repo, jwtGen);
    }

    [Fact]
    public async Task Register_WithValidInput_ReturnsToken()
    {
        await using var db = CreateDb();
        db.Database.EnsureCreated();
        var config = CreateConfig();
        var service = CreateAuthService(db, config);

        var result = await service.RegisterAsync(new RegisterRequest("test@example.com", "Password123"));

        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.False(string.IsNullOrEmpty(result.Token));
        var count = await db.Producers.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsNull()
    {
        await using var db = CreateDb();
        db.Database.EnsureCreated();
        var config = CreateConfig();
        var service = CreateAuthService(db, config);

        await service.RegisterAsync(new RegisterRequest("dup@example.com", "Pass1"));
        var result = await service.RegisterAsync(new RegisterRequest("dup@example.com", "Pass2"));

        Assert.Null(result);
        var count = await db.Producers.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        await using var db = CreateDb();
        db.Database.EnsureCreated();
        var config = CreateConfig();
        var service = CreateAuthService(db, config);
        await service.RegisterAsync(new RegisterRequest("login@example.com", "Secret123"));

        var result = await service.LoginAsync(new LoginRequest("login@example.com", "Secret123"));

        Assert.NotNull(result);
        Assert.Equal("login@example.com", result.Email);
        Assert.False(string.IsNullOrEmpty(result.Token));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsNull()
    {
        await using var db = CreateDb();
        db.Database.EnsureCreated();
        var config = CreateConfig();
        var service = CreateAuthService(db, config);
        await service.RegisterAsync(new RegisterRequest("user@example.com", "RightPass"));

        var result = await service.LoginAsync(new LoginRequest("user@example.com", "WrongPass"));

        Assert.Null(result);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsNull()
    {
        await using var db = CreateDb();
        db.Database.EnsureCreated();
        var config = CreateConfig();
        var service = CreateAuthService(db, config);

        var result = await service.LoginAsync(new LoginRequest("nonexistent@example.com", "AnyPass"));

        Assert.Null(result);
    }
}
