using Analysis.Api.Data;
using Analysis.Api.Services;
using Microsoft.EntityFrameworkCore;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AnalysisDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Host=localhost;Port=5432;Database=analysis_db;Username=agro;Password=secret"));

var influxUrl = builder.Configuration["InfluxDB:Url"];
var influxToken = builder.Configuration["InfluxDB:Token"];
if (!string.IsNullOrWhiteSpace(influxUrl) && !string.IsNullOrWhiteSpace(influxToken))
    builder.Services.AddSingleton<ISensorReadingsTimeSeriesStore, InfluxDbSensorReadingsStore>();
else
    builder.Services.AddSingleton<ISensorReadingsTimeSeriesStore, NullSensorReadingsTimeSeriesStore>();

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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AnalysisDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Analysis API v1"));
app.UseHttpMetrics();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapMetrics();

app.Run();

public partial class Program { }
