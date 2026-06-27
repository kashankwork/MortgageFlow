using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace MortgageFlow.Api.IntegrationTests;

public sealed class HealthEndpointTests : IClassFixture<MortgageFlowApiFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(MortgageFlowApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetLiveHealth_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetReadyHealth_WhenSqlIsReachable_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetLiveHealth_DoesNotRequireSqlConnection()
    {
        // Liveness should only prove that the API process can respond; SQL readiness is tested separately.
        var originalConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        var originalIssuer = Environment.GetEnvironmentVariable("Jwt__Issuer");
        var originalAudience = Environment.GetEnvironmentVariable("Jwt__Audience");
        var originalSigningKey = Environment.GetEnvironmentVariable("Jwt__SigningKey");
        var originalMigrations = Environment.GetEnvironmentVariable("Database__ApplyMigrationsOnStartup");
        var originalSeed = Environment.GetEnvironmentVariable("Seed__DemoPassword");

        try
        {
            Environment.SetEnvironmentVariable(
                "ConnectionStrings__DefaultConnection",
                "Server=127.0.0.1,1;Database=NoSqlNeeded;User Id=sa;Password=not-used;TrustServerCertificate=True;Encrypt=True");
            Environment.SetEnvironmentVariable("Jwt__Issuer", "MortgageFlow.Tests");
            Environment.SetEnvironmentVariable("Jwt__Audience", "MortgageFlow.Api.Tests");
            Environment.SetEnvironmentVariable("Jwt__SigningKey", "integration-test-signing-key-32-chars-min");
            Environment.SetEnvironmentVariable("Database__ApplyMigrationsOnStartup", "false");
            Environment.SetEnvironmentVariable("Seed__DemoPassword", null);

            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection([]));
            });

            var client = factory.CreateClient();
            var response = await client.GetAsync("/health/live");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", originalConnectionString);
            Environment.SetEnvironmentVariable("Jwt__Issuer", originalIssuer);
            Environment.SetEnvironmentVariable("Jwt__Audience", originalAudience);
            Environment.SetEnvironmentVariable("Jwt__SigningKey", originalSigningKey);
            Environment.SetEnvironmentVariable("Database__ApplyMigrationsOnStartup", originalMigrations);
            Environment.SetEnvironmentVariable("Seed__DemoPassword", originalSeed);
        }
    }
}
