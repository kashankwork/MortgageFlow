using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MortgageFlow.Application.Authentication;
using MortgageFlow.Domain;
using MortgageFlow.Infrastructure.Identity;
using MortgageFlow.Infrastructure.Persistence.Entities;

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

        var broker = await UpsertSyntheticUserAsync(
            "broker@example.test",
            "Synthetic Broker",
            MortgageFlowRoles.Broker,
            demoPassword);
        var processor = await UpsertSyntheticUserAsync(
            "processor@example.test",
            "Synthetic Processor",
            MortgageFlowRoles.Processor,
            demoPassword,
            capacityPoints: 8);
        var processor2 = await UpsertSyntheticUserAsync(
            "processor.secondary@example.test",
            "Synthetic Processor Secondary",
            MortgageFlowRoles.Processor,
            demoPassword,
            capacityPoints: 12);
        var underwriter = await UpsertSyntheticUserAsync(
            "underwriter@example.test",
            "Synthetic Underwriter",
            MortgageFlowRoles.Underwriter,
            demoPassword,
            capacityPoints: 8);
        var underwriter2 = await UpsertSyntheticUserAsync(
            "underwriter.secondary@example.test",
            "Synthetic Underwriter Secondary",
            MortgageFlowRoles.Underwriter,
            demoPassword,
            capacityPoints: 10);
        await UpsertSyntheticUserAsync(
            "teamlead@example.test",
            "Synthetic Team Lead",
            MortgageFlowRoles.TeamLead,
            demoPassword);

        await SeedSkillAsync(processor.Id, "processing");
        await SeedSkillAsync(processor2.Id, "processing");
        await SeedSkillAsync(underwriter.Id, "underwriting");
        await SeedSkillAsync(underwriter2.Id, "underwriting");

        await SeedSyntheticWorkflowLoansAsync(broker.Id, processor.Id, processor2.Id, underwriter.Id, underwriter2.Id);
    }

    private async Task<ApplicationUser> UpsertSyntheticUserAsync(
        string email,
        string fullName,
        string role,
        string password,
        int capacityPoints = 8)
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
                CapacityPoints = capacityPoints
            };

            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException($"Could not seed synthetic user {email}: {FormatErrors(createResult)}");
            }
        }
        else
        {
            user.FullName = fullName;
            user.TeamId = DemoTeamId;
            user.IsActive = true;
            user.IsAvailable = true;
            user.CapacityPoints = capacityPoints;
            await _userManager.UpdateAsync(user);
        }

        if (!await _userManager.IsInRoleAsync(user, role))
        {
            var roleResult = await _userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException($"Could not assign role {role} to {email}: {FormatErrors(roleResult)}");
            }
        }

        return user;
    }

    private static string FormatErrors(IdentityResult result)
    {
        return string.Join("; ", result.Errors.Select(error => error.Description));
    }

    private async Task SeedSkillAsync(Guid userId, string skillTag)
    {
        var exists = await _dbContext.EmployeeSkills.AnyAsync(skill =>
            skill.UserId == userId && skill.SkillTag == skillTag);
        if (exists)
        {
            return;
        }

        _dbContext.EmployeeSkills.Add(new EmployeeSkill
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SkillTag = skillTag
        });
        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedSyntheticWorkflowLoansAsync(
        Guid brokerId,
        Guid processorId,
        Guid processor2Id,
        Guid underwriterId,
        Guid underwriter2Id)
    {
        // Seed timelines sit safely in the past so manual workflow actions remain chronological.
        var now = DateTime.UtcNow.AddHours(-2);
        var submitted = await SeedLoanIfMissingAsync(
            "MF-900001",
            brokerId,
            now,
            "Synthetic Submitted Borrower",
            "submitted.borrower@example.test",
            loan =>
            {
                loan.TransitionTo(LoanStatus.Submitted, brokerId, null, now.AddMinutes(4));
                loan.AssignTo(processorId, now.AddMinutes(5));
            });
        var underwriting = await SeedLoanIfMissingAsync(
            "MF-900002",
            brokerId,
            now.AddMinutes(10),
            "Synthetic Underwriting Borrower",
            "underwriting.borrower@example.test",
            loan =>
            {
                loan.TransitionTo(LoanStatus.Submitted, brokerId, null, now.AddMinutes(14));
                loan.AssignTo(processorId, now.AddMinutes(15));
                loan.TransitionTo(LoanStatus.Processing, processorId, null, now.AddMinutes(16));
                loan.TransitionTo(LoanStatus.Underwriting, processorId, "Synthetic checklist complete.", now.AddMinutes(17));
                loan.AssignTo(underwriterId, now.AddMinutes(18));
            });
        var unassignedSubmitted = await SeedLoanIfMissingAsync(
            "MF-900003",
            brokerId,
            now.AddMinutes(20),
            "Synthetic Queue Borrower",
            "queue.borrower@example.test",
            loan => loan.TransitionTo(LoanStatus.Submitted, brokerId, null, now.AddMinutes(24)));
        await SeedLoanIfMissingAsync(
            "MF-900004",
            brokerId,
            now.AddMinutes(30),
            "Synthetic Underwriting Queue Borrower",
            "underwriting.queue.borrower@example.test",
            loan =>
            {
                loan.TransitionTo(LoanStatus.Submitted, brokerId, null, now.AddMinutes(34));
                loan.TransitionTo(LoanStatus.Processing, processor2Id, null, now.AddMinutes(35));
                loan.TransitionTo(LoanStatus.Underwriting, processor2Id, "Synthetic checklist complete.", now.AddMinutes(36));
            });

        await _dbContext.SaveChangesAsync();

        await SeedTaskIfMissingAsync(submitted.Id, processorId, BusinessPriority.Normal, now.AddDays(1));
        await SeedTaskIfMissingAsync(underwriting.Id, underwriterId, BusinessPriority.High, now.AddDays(2));
        await SeedTaskIfMissingAsync(unassignedSubmitted.Id, processorId, BusinessPriority.Urgent, DateTime.UtcNow.AddHours(12));
        await SeedTaskIfMissingAsync(unassignedSubmitted.Id, processor2Id, BusinessPriority.Normal, DateTime.UtcNow.AddDays(3));
    }

    private async Task<LoanApplication> SeedLoanIfMissingAsync(
        string loanNumberValue,
        Guid brokerId,
        DateTime createdUtc,
        string borrowerName,
        string borrowerEmail,
        Action<LoanApplication> configure)
    {
        var loanNumber = LoanNumber.Create(loanNumberValue);
        var existing = await _dbContext.LoanApplications.SingleOrDefaultAsync(loan => loan.LoanNumber == loanNumber);
        if (existing is not null)
        {
            return existing;
        }

        var loan = CreateCompleteSyntheticLoan(loanNumber, brokerId, createdUtc, borrowerName, borrowerEmail);
        configure(loan);
        _dbContext.LoanApplications.Add(loan);
        return loan;
    }

    private async Task SeedTaskIfMissingAsync(
        Guid loanId,
        Guid assigneeId,
        BusinessPriority priority,
        DateTime dueUtc)
    {
        var exists = await _dbContext.WorkflowTasks.AnyAsync(task =>
            task.LoanApplicationId == loanId && task.AssigneeId == assigneeId && task.Priority == priority);
        if (exists)
        {
            return;
        }

        _dbContext.WorkflowTasks.Add(new WorkflowTask
        {
            Id = Guid.NewGuid(),
            LoanApplicationId = loanId,
            AssigneeId = assigneeId,
            Priority = priority,
            DueUtc = dueUtc,
            IsComplete = false
        });
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
