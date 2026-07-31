using System.Net;
using System.Text.Json;
using Xunit;

namespace ATT.Monitor.Api.Tests.Integration;

/// <summary>Humo HTTP sin dependencia de SQL (liveness + endpoint público).</summary>
public sealed class SmokeTests(MonitorApiFixture factory) : IClassFixture<MonitorApiFixture>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task HealthLive_returns_200()
    {
        var response = await _client.GetAsync(new Uri("/health/live", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PublicUploadLimits_returns_json_with_max_megabytes()
    {
        var response = await _client.GetAsync(new Uri("/api/public/upload-limits", UriKind.Relative));
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("maxMegabytes").TryGetInt32(out var mb));
        Assert.InRange(mb, 32, 2048);
        Assert.True(root.GetProperty("maxBytes").TryGetInt64(out var bytes));
        Assert.True(bytes > 0);
    }

    [Fact]
    public async Task SystemInfo_returns_json_with_service_name()
    {
        var response = await _client.GetAsync(new Uri("/api/system/info", UriKind.Relative));
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;
        Assert.Equal("ATT.Monitor.Api", root.GetProperty("service").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("version").GetString()));
    }
}
