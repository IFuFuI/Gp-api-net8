using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ATT.Monitor.Api.Tests.Integration;

/// <summary>
/// Host de prueba con configuración estable para <see cref="WebApplicationFactory{TEntryPoint}"/>.
/// </summary>
public sealed class MonitorApiFixture : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["TestHost:DisableHttpsRedirection"] = "true",
                });
        });
    }
}
