using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MortgageFlow.Application.Authentication;
using MortgageFlow.Infrastructure.Persistence;

namespace MortgageFlow.Api.IntegrationTests;

public sealed class AuthenticationEndpointTests : IClassFixture<MortgageFlowApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly MortgageFlowApiFactory _factory;
    private readonly HttpClient _client;

    public AuthenticationEndpointTests(MortgageFlowApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidSyntheticAccount_ReturnsJwtWithRoleClaim()
    {
        var login = await LoginAsync("broker@example.test", MortgageFlowApiFactory.DemoPassword);

        Assert.Equal(MortgageFlowRoles.Broker, login.Role);
        Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));

        var token = new JwtSecurityTokenHandler().ReadJwtToken(login.AccessToken);
        // Reading the token verifies the role claim is actually embedded, not only echoed in the response body.
        Assert.Contains(token.Claims, claim => claim.Value == MortgageFlowRoles.Broker);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorizedWithoutAccountDisclosure()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "broker@example.test", password = "wrong-password" });

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain("broker@example.test", problem.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_WithInactiveAccount_ReturnsUnauthorized()
    {
        await DeactivateUserAsync("processor@example.test");

        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "processor@example.test", password = MortgageFlowApiFactory.DemoPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TeamLeadPolicy_WithBrokerToken_ReturnsForbidden()
    {
        var login = await LoginAsync("broker@example.test", MortgageFlowApiFactory.DemoPassword);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        // This confirms authentication succeeded but authorization failed because the role is wrong.
        var response = await _client.GetAsync("/api/v1/auth/team-lead-check");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsCurrentUser()
    {
        var login = await LoginAsync("teamlead@example.test", MortgageFlowApiFactory.DemoPassword);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var response = await _client.GetAsync("/api/v1/auth/me");
        var currentUser = await response.Content.ReadFromJsonAsync<CurrentUserResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(MortgageFlowRoles.TeamLead, currentUser?.Role);
    }

    private async Task<LoginResponse> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions)
            ?? throw new InvalidOperationException("Login response was empty.");
    }

    private async Task DeactivateUserAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MortgageFlowDbContext>();
        var user = await dbContext.Users.SingleAsync(x => x.Email == email);

        user.IsActive = false;
        await dbContext.SaveChangesAsync();
    }

    private sealed record LoginResponse(
        Guid UserId,
        string Email,
        string FullName,
        string Role,
        string AccessToken,
        DateTimeOffset ExpiresUtc);

    private sealed record CurrentUserResponse(Guid UserId, string Email, string Role);
}
