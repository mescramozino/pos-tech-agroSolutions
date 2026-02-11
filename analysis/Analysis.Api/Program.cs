using Analysis.Api.Data;
using Analysis.Api.Services;
using Microsoft.EntityFrameworkCore;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AnalysisDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Host=localhost;Port=5432;Database=analysis_db;Username=agro;Password=secret"));

builder.Services.AddScoped<ISensorReadingsTimeSeriesStore, PostgresSensorReadingsStore>();

builder.Services.AddHttpClient();
builder.Services.AddHostedService<SensorReadingsConsumer>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Analysis API", Version = "v1", Description = "Análise e alertas (AgroSolutions IoT)." });
});
builder.Services.AddHealthChecks();

var app = builder.Build();

await EnsureDatabaseAsync(app);

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Analysis API v1"));
app.UseHttpMetrics();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapMetrics();

app.Run();

public partial class Program
{
    static async Task EnsureDatabaseAsync(WebApplication app)
    {
        const int maxAttempts = 15;
        const int delayMs = 2000;
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AnalysisDbContext>();
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await db.Database.EnsureCreatedAsync();
                return;
            }
            catch (Exception)
            {
                if (attempt == maxAttempts) throw;
                await Task.Delay(delayMs);
            }
        }
    }
}
