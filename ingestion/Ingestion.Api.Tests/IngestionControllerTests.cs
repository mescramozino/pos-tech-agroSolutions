using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Ingestion.Application;
using Xunit;

namespace Ingestion.Api.Tests;

public class IngestionControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public IngestionControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostSensors_ValidPayload_Returns202Accepted()
    {
        var request = new SensorIngestionRequest(
            PlotId: Guid.NewGuid(),
            Readings: new List<SensorReadingDto>
            {
                new("moisture", 45.5, DateTime.UtcNow),
                new("temperature", 28.0, DateTime.UtcNow)
            });
        var response = await _client.PostAsJsonAsync("/api/ingestion/sensors", request, JsonOptions);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task PostSensors_EmptyPlotId_Returns400BadRequest()
    {
        var request = new SensorIngestionRequest(
            PlotId: Guid.Empty,
            Readings: new List<SensorReadingDto> { new("moisture", 50, DateTime.UtcNow) });
        var response = await _client.PostAsJsonAsync("/api/ingestion/sensors", request, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostSensors_InvalidType_Returns400BadRequest()
    {
        var request = new SensorIngestionRequest(
            PlotId: Guid.NewGuid(),
            Readings: new List<SensorReadingDto> { new("invalid_type", 50, DateTime.UtcNow) });
        var response = await _client.PostAsJsonAsync("/api/ingestion/sensors", request, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
