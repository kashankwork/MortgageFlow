using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MortgageFlow.Infrastructure.Persistence;

namespace MortgageFlow.Api.IntegrationTests;

public sealed class MortgageFlowApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string DemoPassword = "SyntheticDemo!12345";

    // Each test fixture gets its own database so SQL state is repeatable and isolated.
    private readonly string _databaseName = $"MortgageFlowTests_{Guid.NewGuid():N}";
    private readonly Dictionary<string, string?> _originalEnvironment = [];

    public string ConnectionString => BuildConnectionString(_databaseName);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                ["Jwt:Issuer"] = "MortgageFlow.Tests",
                ["Jwt:Audience"] = "MortgageFlow.Api.Tests",
                ["Jwt:SigningKey"] = "integration-test-signing-key-32-chars-min",
                ["Jwt:ExpiryMinutes"] = "60",
                ["Seed:DemoPassword"] = DemoPassword,
                ["Database:ApplyMigrationsOnStartup"] = "true"
            });
        });
    }

    public Task InitializeAsync()
    {
        // Minimal hosting reads some configuration before ConfigureWebHost is fully applied,
        // so tests set process-level overrides before the test server starts.
        SetEnvironmentVariable("ConnectionStrings__DefaultConnection", ConnectionString);
        SetEnvironmentVariable("Jwt__Issuer", "MortgageFlow.Tests");
        SetEnvironmentVariable("Jwt__Audience", "MortgageFlow.Api.Tests");
        SetEnvironmentVariable("Jwt__SigningKey", "integration-test-signing-key-32-chars-min");
        SetEnvironmentVariable("Jwt__ExpiryMinutes", "60");
        SetEnvironmentVariable("Seed__DemoPassword", DemoPassword);
        SetEnvironmentVariable("Database__ApplyMigrationsOnStartup", "true");

        _ = CreateClient();
        return Task.CompletedTask;
    }

    public new async Task DisposeAsync()
    {
        try
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MortgageFlowDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
        }
        finally
        {
            foreach (var (name, value) in _originalEnvironment)
            {
                Environment.SetEnvironmentVariable(name, value);
            }
        }

        await base.DisposeAsync();
    }

    private void SetEnvironmentVariable(string name, string value)
    {
        _originalEnvironment.TryAdd(name, Environment.GetEnvironmentVariable(name));
        Environment.SetEnvironmentVariable(name, value);
    }

    private static string BuildConnectionString(string databaseName)
    {
        var explicitConnectionString = Environment.GetEnvironmentVariable("MORTGAGEFLOW_TEST_SQL_CONNECTION");
        if (!string.IsNullOrWhiteSpace(explicitConnectionString))
        {
            // CI or a developer can provide a full base connection string; tests still choose the database name.
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(explicitConnectionString)
            {
                InitialCatalog = databaseName
            };

            return builder.ConnectionString;
        }

        var sqlPassword = Environment.GetEnvironmentVariable("MORTGAGEFLOW_SQL_PASSWORD");
        if (string.IsNullOrWhiteSpace(sqlPassword))
        {
            throw new InvalidOperationException(
                "Set MORTGAGEFLOW_SQL_PASSWORD or MORTGAGEFLOW_TEST_SQL_CONNECTION before running SQL-backed integration tests.");
        }

        return new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
        {
            DataSource = "127.0.0.1,14333",
            InitialCatalog = databaseName,
            UserID = "sa",
            Password = sqlPassword,
            Encrypt = true,
            TrustServerCertificate = true
        }.ConnectionString;
    }
}
