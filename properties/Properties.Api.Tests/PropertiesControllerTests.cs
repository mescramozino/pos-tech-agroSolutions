using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Properties.Application;
using Xunit;

namespace Properties.Api.Tests;

public class PropertiesControllerTests : IClassFixture<PropertiesApiFixture>
{
    private readonly HttpClient _client;
    private static readonly Guid TestProducerId = Guid.NewGuid();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public PropertiesControllerTests(PropertiesApiFixture factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Producer-Id", TestProducerId.ToString());
    }

    [Fact]
    public async Task CreateProperty_ReturnsCreated()
    {
        var request = new CreatePropertyRequest("Fazenda Teste", "Região Sul");
        var response = await _client.PostAsJsonAsync("/api/properties", request, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PropertyResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Fazenda Teste", body.Name);
        Assert.Equal(TestProducerId, body.ProducerId);
    }

    [Fact]
    public async Task GetProperties_ReturnsList()
    {
        var response = await _client.GetAsync("/api/properties");
        response.EnsureSuccessStatusCode();
        var list = await response.Content.ReadFromJsonAsync<List<PropertyResponse>>(JsonOptions);
        Assert.NotNull(list);
    }

    [Fact]
    public async Task CreateProperty_AndCreatePlot_ReturnsCreated()
    {
        var propRequest = new CreatePropertyRequest("Fazenda Plot Test", null);
        var propResponse = await _client.PostAsJsonAsync("/api/properties", propRequest, JsonOptions);
        propResponse.EnsureSuccessStatusCode();
        var property = await propResponse.Content.ReadFromJsonAsync<PropertyResponse>(JsonOptions);
        Assert.NotNull(property);

        var plotRequest = new CreatePlotRequest("Talhão 1", "Soja");
        var plotResponse = await _client.PostAsJsonAsync($"/api/properties/{property.Id}/plots", plotRequest, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, plotResponse.StatusCode);
        var plot = await plotResponse.Content.ReadFromJsonAsync<PlotResponse>(JsonOptions);
        Assert.NotNull(plot);
        Assert.Equal(property.Id, plot.PropertyId);
        Assert.Equal("Soja", plot.Culture);
    }
}
