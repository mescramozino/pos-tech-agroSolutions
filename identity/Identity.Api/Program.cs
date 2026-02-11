using Microsoft.EntityFrameworkCore;
using Prometheus;
using Identity.Application.Interfaces;
using Identity.Application.Services;
using Identity.Domain;
using Identity.Infrastructure.Data;
using Identity.Infrastructure.Repositories;
using Identity.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Host=localhost;Port=5432;Database=identity_db;Username=agro;Password=secret"));
builder.Services.AddScoped<IProducerRepository, ProducerRepository>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Identity API", Version = "v1", Description = "Autenticação e registro de produtores (AgroSolutions IoT)." });
});
builder.Services.AddHealthChecks();

var app = builder.Build();

await EnsureDatabaseAndSeedAsync(app);

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity API v1"));
app.UseHttpMetrics();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapMetrics();

app.Run();

static async Task EnsureDatabaseAndSeedAsync(WebApplication app)
{
    const int maxAttempts = 15;
    const int delayMs = 2000;
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await db.Database.EnsureCreatedAsync();
            if (!await db.Producers.AnyAsync())
            {
                var producerId = new Guid("A1000000-1000-1000-1000-000000000001");
                var producer = new Producer
                {
                    Id = producerId,
                    Email = "produtor@agro.local",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Senha123!"),
                    CreatedAt = DateTime.UtcNow
                };
                db.Producers.Add(producer);
                await db.SaveChangesAsync();
            }
            return;
        }
        catch (Exception)
        {
            if (attempt == maxAttempts) throw;
            await Task.Delay(delayMs);
        }
    }
}
