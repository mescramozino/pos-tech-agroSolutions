using Ingestion.Application;
using Ingestion.Infrastructure;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ISensorReadingsStore, SensorReadingsStore>();
var rabbitHost = builder.Configuration["RabbitMQ:Host"];
if (!string.IsNullOrWhiteSpace(rabbitHost))
    builder.Services.AddSingleton<ISensorReadingsPublisher, RabbitMqSensorReadingsPublisher>();
else
    builder.Services.AddSingleton<ISensorReadingsPublisher, NullSensorReadingsPublisher>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Ingestion API", Version = "v1", Description = "Ingestão de dados de sensores (AgroSolutions IoT)." });
});
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ingestion API v1"));
app.UseHttpMetrics();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapMetrics();

app.Run();

public partial class Program { }
