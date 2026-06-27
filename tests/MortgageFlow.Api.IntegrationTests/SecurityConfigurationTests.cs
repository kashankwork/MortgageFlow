using System.Text.Json;

namespace MortgageFlow.Api.IntegrationTests;

public sealed class SecurityConfigurationTests
{
    [Fact]
    public async Task AppSettings_DoNotContainRealSecrets()
    {
        // Public config files should declare keys, but actual secret values must come from env/user-secrets.
        var root = FindRepositoryRoot();
        var appSettings = await File.ReadAllTextAsync(Path.Combine(root, "src/MortgageFlow.Api/appsettings.json"));
        var developmentSettings = await File.ReadAllTextAsync(Path.Combine(root, "src/MortgageFlow.Api/appsettings.Development.json"));

        Assert.Empty(ReadJsonString(appSettings, "ConnectionStrings", "DefaultConnection"));
        Assert.Empty(ReadJsonString(appSettings, "Jwt", "SigningKey"));
        Assert.Empty(ReadJsonString(developmentSettings, "ConnectionStrings", "DefaultConnection"));
        Assert.Empty(ReadJsonString(developmentSettings, "Jwt", "SigningKey"));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MortgageFlow.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate MortgageFlow.sln.");
    }

    private static string ReadJsonString(string json, params string[] path)
    {
        using var document = JsonDocument.Parse(json);
        var element = document.RootElement;

        foreach (var segment in path)
        {
            element = element.GetProperty(segment);
        }

        return element.GetString() ?? string.Empty;
    }
}
