using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MortgageFlow.Application.Authentication;
using MortgageFlow.Domain;
using MortgageFlow.Infrastructure.Identity;

namespace MortgageFlow.Infrastructure.Persistence;

public sealed class DevelopmentDataSeeder
{
    private static readonly Guid DemoTeamId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly MortgageFlowDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DevelopmentDataSeeder> _logger;

    public DevelopmentDataSeeder(
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<ApplicationUser> userManager,
        MortgageFlowDbContext dbContext,
        IConfiguration configuration,
        ILogger<DevelopmentDataSeeder> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _dbContext = dbContext;
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

        await SeedSyntheticWorkflowLoansAsync();
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

    private async Task SeedSyntheticWorkflowLoansAsync()
    {
        var submittedDemoLoanNumber = LoanNumber.Create("MF-900001");
        if (await _dbContext.LoanApplications
            .AnyAsync(loan => loan.LoanNumber == submittedDemoLoanNumber))
        {
            return;
        }

        var broker = await _userManager.FindByEmailAsync("broker@example.test")
            ?? throw new InvalidOperationException("Synthetic broker was not seeded.");
        var processor = await _userManager.FindByEmailAsync("processor@example.test")
            ?? throw new InvalidOperationException("Synthetic processor was not seeded.");
        var underwriter = await _userManager.FindByEmailAsync("underwriter@example.test")
            ?? throw new InvalidOperationException("Synthetic underwriter was not seeded.");

        // Seed timelines sit safely in the past so manual workflow actions remain chronological.
        var now = DateTime.UtcNow.AddHours(-2);
        var submitted = CreateCompleteSyntheticLoan(
            submittedDemoLoanNumber,
            broker.Id,
            now,
            "Synthetic Submitted Borrower",
            "submitted.borrower@example.test");
        submitted.TransitionTo(LoanStatus.Submitted, broker.Id, null, now.AddMinutes(4));
        submitted.AssignTo(processor.Id, now.AddMinutes(5));

        var underwriting = CreateCompleteSyntheticLoan(
            LoanNumber.Create("MF-900002"),
            broker.Id,
            now.AddMinutes(10),
            "Synthetic Underwriting Borrower",
            "underwriting.borrower@example.test");
        underwriting.TransitionTo(LoanStatus.Submitted, broker.Id, null, now.AddMinutes(14));
        underwriting.AssignTo(processor.Id, now.AddMinutes(15));
        underwriting.TransitionTo(LoanStatus.Processing, processor.Id, null, now.AddMinutes(16));
        underwriting.TransitionTo(LoanStatus.Underwriting, processor.Id, "Synthetic checklist complete.", now.AddMinutes(17));
        underwriting.AssignTo(underwriter.Id, now.AddMinutes(18));

        _dbContext.LoanApplications.AddRange(submitted, underwriting);
        await _dbContext.SaveChangesAsync();
    }

    private static LoanApplication CreateCompleteSyntheticLoan(
        LoanNumber loanNumber,
        Guid brokerId,
        DateTime createdUtc,
        string borrowerName,
        string borrowerEmail)
    {
        var loan = LoanApplication.CreateDraft(loanNumber, brokerId, Money.Usd(275_000), createdUtc);
        loan.AddBorrower(Borrower.Create(borrowerName, borrowerEmail, Money.Usd(120_000)), createdUtc.AddMinutes(1));
        loan.AddProperty(
            Property.Create(
                "123 Synthetic Lane",
                "Pontiac",
                "MI",
                "48341",
                Money.Usd(350_000),
                OccupancyType.PrimaryResidence),
            createdUtc.AddMinutes(2));
        loan.UpdateLoanTerms(Money.Usd(275_000), LoanPurpose.Purchase, 6.875m, 360, createdUtc.AddMinutes(3));

        return loan;
    }
}
