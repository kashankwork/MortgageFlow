using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MortgageFlow.Application.Authentication;
using MortgageFlow.Infrastructure.Identity;

namespace MortgageFlow.Infrastructure.Persistence;

public sealed class DevelopmentDataSeeder
{
    private static readonly Guid DemoTeamId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DevelopmentDataSeeder> _logger;

    public DevelopmentDataSeeder(
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger<DevelopmentDataSeeder> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var demoPassword = _configuration["Seed:DemoPassword"];
        if (string.IsNullOrWhiteSpace(demoPassword))
        {
            // Avoid creating known accounts unless the developer explicitly opts into demo seeding.
            _logger.LogWarning("Synthetic demo users were not seeded because Seed:DemoPassword is not configured.");
            return;
        }

        foreach (var role in MortgageFlowRoles.All)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        await UpsertSyntheticUserAsync("broker@example.test", "Synthetic Broker", MortgageFlowRoles.Broker, demoPassword);
        await UpsertSyntheticUserAsync("processor@example.test", "Synthetic Processor", MortgageFlowRoles.Processor, demoPassword);
        await UpsertSyntheticUserAsync("underwriter@example.test", "Synthetic Underwriter", MortgageFlowRoles.Underwriter, demoPassword);
        await UpsertSyntheticUserAsync("teamlead@example.test", "Synthetic Team Lead", MortgageFlowRoles.TeamLead, demoPassword);
    }

    private async Task UpsertSyntheticUserAsync(string email, string fullName, string role, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            // These accounts are fake demo actors only; no real borrower or employee data is seeded.
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                TeamId = DemoTeamId,
                IsActive = true,
                IsAvailable = true,
                CapacityPoints = 8
            };

            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException($"Could not seed synthetic user {email}: {FormatErrors(createResult)}");
            }
        }

        if (!await _userManager.IsInRoleAsync(user, role))
        {
            var roleResult = await _userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException($"Could not assign role {role} to {email}: {FormatErrors(roleResult)}");
            }
        }
    }

    private static string FormatErrors(IdentityResult result)
    {
        return string.Join("; ", result.Errors.Select(error => error.Description));
    }
}
