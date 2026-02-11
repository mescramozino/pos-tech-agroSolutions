using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Analysis.Api.Controllers;

[ApiController]
[Route("api/weather")]
public class WeatherController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WeatherController> _logger;
    private const string GeocodingUrl = "https://geocoding-api.open-meteo.com/v1/search";
    private const string ForecastUrl = "https://api.open-meteo.com/v1/forecast";

    public WeatherController(IHttpClientFactory httpClientFactory, ILogger<WeatherController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet("forecast")]
    public async Task<ActionResult<WeatherForecastDto>> GetForecast(
        [FromQuery] string? city,
        [FromQuery] double? lat,
        [FromQuery] double? lon,
        CancellationToken ct)
    {
        double latitude, longitude;
        string locationName = "Previsão";

        if (lat.HasValue && lon.HasValue)
        {
            latitude = lat.Value;
            longitude = lon.Value;
        }
        else if (!string.IsNullOrWhiteSpace(city))
        {
            var (foundLat, foundLon, name) = await GeocodeAsync(city.Trim(), ct);
            if (foundLat == null) return BadRequest("Cidade não encontrada.");
            latitude = foundLat.Value;
            longitude = foundLon!.Value;
            locationName = name ?? city;
        }
        else
        {
            latitude = -23.55;
            longitude = -46.63;
            locationName = "São Paulo";
        }

        var dto = await FetchForecastAsync(latitude, longitude, locationName, ct);
        if (dto == null) return StatusCode(502, "Serviço de previsão indisponível.");
        return Ok(dto);
    }

    private async Task<(double? lat, double? lon, string? name)> GeocodeAsync(string city, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{GeocodingUrl}?name={Uri.EscapeDataString(city)}&count=1", ct);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
            {
                var first = results[0];
                var lat = first.GetProperty("latitude").GetDouble();
                var lng = first.GetProperty("longitude").GetDouble();
                var name = first.TryGetProperty("name", out var n) ? n.GetString() : city;
                return (lat, lng, name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Geocoding failed for city={City}", city);
        }
        return (null, null, null);
    }

    private async Task<WeatherForecastDto?> FetchForecastAsync(double lat, double lon, string locationName, CancellationToken ct)
    {
        try
        {
            var url = $"{ForecastUrl}?latitude={lat}&longitude={lon}&current=temperature_2m,relative_humidity_2m,precipitation,weather_code&daily=temperature_2m_max,temperature_2m_min,precipitation_sum&timezone=America/Sao_Paulo";
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);
            var current = doc.RootElement.GetProperty("current");
            var daily = doc.RootElement.GetProperty("daily");
            var times = daily.GetProperty("time");
            var maxT = daily.GetProperty("temperature_2m_max");
            var minT = daily.GetProperty("temperature_2m_min");
            var precip = daily.GetProperty("precipitation_sum");
            var dailyList = new List<DailyForecastDto>();
            for (var i = 0; i < Math.Min(5, times.GetArrayLength()); i++)
            {
                dailyList.Add(new DailyForecastDto(
                    times[i].GetString()!,
                    maxT[i].GetDouble(),
                    minT[i].GetDouble(),
                    precip[i].GetDouble()));
            }
            return new WeatherForecastDto(
                locationName,
                current.GetProperty("temperature_2m").GetDouble(),
                current.GetProperty("relative_humidity_2m").GetDouble(),
                current.GetProperty("precipitation").GetDouble(),
                current.GetProperty("weather_code").GetInt32(),
                dailyList);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Forecast fetch failed for lat={Lat}, lon={Lon}", lat, lon);
            return null;
        }
    }
}

public record WeatherForecastDto(
    string Location,
    double TemperatureC,
    double HumidityPercent,
    double PrecipitationMm,
    int WeatherCode,
    IReadOnlyList<DailyForecastDto> Daily);

public record DailyForecastDto(string Date, double MaxTempC, double MinTempC, double PrecipitationMm);
