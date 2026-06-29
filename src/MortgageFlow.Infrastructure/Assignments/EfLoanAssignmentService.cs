using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MortgageFlow.Application.Assignments;
using MortgageFlow.Application.Authentication;
using MortgageFlow.Application.Diagnostics;
using MortgageFlow.Application.Loans;
using MortgageFlow.Application.Users;
using MortgageFlow.Domain;
using MortgageFlow.Infrastructure.Identity;
using MortgageFlow.Infrastructure.Persistence;
using MortgageFlow.Infrastructure.Persistence.Entities;

namespace MortgageFlow.Infrastructure.Assignments;

public sealed class EfLoanAssignmentService : ILoanAssignmentService
{
    private readonly MortgageFlowDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ILoanPriorityCalculator _priorityCalculator;
    private readonly IAssignmentStrategy _assignmentStrategy;
    private readonly IEmployeeEligibilityPolicy _eligibilityPolicy;
    private readonly IEmployeeWorkloadCalculator _workloadCalculator;
    private readonly ICorrelationContext _correlationContext;
    private readonly AssignmentOptions _options;

    public EfLoanAssignmentService(
        MortgageFlowDbContext dbContext,
        ICurrentUser currentUser,
        ILoanPriorityCalculator priorityCalculator,
        IAssignmentStrategy assignmentStrategy,
        IEmployeeEligibilityPolicy eligibilityPolicy,
        IEmployeeWorkloadCalculator workloadCalculator,
        ICorrelationContext correlationContext,
        IOptions<AssignmentOptions> options)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _priorityCalculator = priorityCalculator;
        _assignmentStrategy = assignmentStrategy;
        _eligibilityPolicy = eligibilityPolicy;
        _workloadCalculator = workloadCalculator;
        _correlationContext = correlationContext;
        _options = options.Value;
    }

    public async Task<LoanActionResult<AssignmentDecisionResponse>> AssignAutomaticallyAsync(
        Guid loanId,
        CancellationToken cancellationToken)
    {
        var actor = await RequireTeamLeadAsync(cancellationToken);
        if (actor is null)
        {
            return LoanActionResult<AssignmentDecisionResponse>.Forbidden("Only team leads can assign loans.");
        }

        var loan = await _dbContext.LoanApplications.SingleOrDefaultAsync(item => item.Id == loanId, cancellationToken);
        if (loan is null)
        {
            return LoanActionResult<AssignmentDecisionResponse>.NotFound();
        }

        if (loan.AssigneeId is not null)
        {
            return LoanActionResult<AssignmentDecisionResponse>.ValidationFailed(
                new Dictionary<string, string[]> { ["Assignee"] = ["Loan is already assigned."] });
        }

        if (!TryCreateAssignmentContext(loan, actor.TeamId, out var context))
        {
            return LoanActionResult<AssignmentDecisionResponse>.ValidationFailed(
                new Dictionary<string, string[]> { ["Status"] = ["Loan status is not assignable."] });
        }

        var candidates = await LoadCandidatesAsync(context, cancellationToken);
        var result = await _assignmentStrategy.SelectAssigneeAsync(context, candidates, cancellationToken);
        if (!result.Assigned || result.Candidate is null || result.Workload is null)
        {
            return LoanActionResult<AssignmentDecisionResponse>.Success(
                MapAssignmentResponse(loan, assigned: false, null, null, null, result.Reason));
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var assignmentId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            loan.AssignTo(result.Candidate.EmployeeId, now);
            AddAssignmentRecord(
                assignmentId,
                loan.Id,
                previousAssigneeId: null,
                result.Candidate.EmployeeId,
                isAutomatic: true,
                actor.Id,
                "Automatic assignment selected the lowest normalized eligible employee.",
                now);
            AddAudit(
                actor.Id,
                "LoanAssigned",
                loan.Id,
                "Loan assigned automatically to an eligible employee.");

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return LoanActionResult<AssignmentDecisionResponse>.Success(
                MapAssignmentResponse(loan, assigned: true, assignmentId, result.Candidate, result.Workload, result.Reason));
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return LoanActionResult<AssignmentDecisionResponse>.Conflict(
                "The loan or assignment cursor was updated by another request. Refresh and retry.");
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return LoanActionResult<AssignmentDecisionResponse>.Conflict(
                "The loan assignment changed while this request was being saved. Refresh and retry.");
        }
    }

    public async Task<LoanActionResult<AssignmentDecisionResponse>> ReassignAsync(
        Guid loanId,
        ManualReassignmentRequest request,
        CancellationToken cancellationToken)
    {
        var actor = await RequireTeamLeadAsync(cancellationToken);
        if (actor is null)
        {
            return LoanActionResult<AssignmentDecisionResponse>.Forbidden("Only team leads can reassign loans.");
        }

        if (request.NewAssigneeId == Guid.Empty || string.IsNullOrWhiteSpace(request.Reason))
        {
            return LoanActionResult<AssignmentDecisionResponse>.ValidationFailed(new Dictionary<string, string[]>
            {
                [nameof(request.NewAssigneeId)] = ["New assignee is required."],
                [nameof(request.Reason)] = ["Manual reassignment reason is required."]
            });
        }

        var loan = await _dbContext.LoanApplications.SingleOrDefaultAsync(item => item.Id == loanId, cancellationToken);
        if (loan is null)
        {
            return LoanActionResult<AssignmentDecisionResponse>.NotFound();
        }

        if (!TryApplyRowVersion(loan, request.RowVersion, out var rowVersionErrors))
        {
            return LoanActionResult<AssignmentDecisionResponse>.ValidationFailed(rowVersionErrors);
        }

        if (!TryCreateAssignmentContext(loan, actor.TeamId, out var context))
        {
            return LoanActionResult<AssignmentDecisionResponse>.ValidationFailed(
                new Dictionary<string, string[]> { ["Status"] = ["Loan status is not assignable."] });
        }

        var candidates = await LoadCandidatesAsync(context, cancellationToken);
        var candidate = candidates.SingleOrDefault(item => item.EmployeeId == request.NewAssigneeId);
        if (candidate is null || !_eligibilityPolicy.IsEligible(candidate, context))
        {
            return LoanActionResult<AssignmentDecisionResponse>.ValidationFailed(
                new Dictionary<string, string[]> { ["NewAssigneeId"] = ["Selected employee is not eligible for this loan."] });
        }

        var workload = _workloadCalculator.Calculate(candidate);
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var assignmentId = Guid.NewGuid();
            var previousAssigneeId = loan.AssigneeId;
            var now = DateTime.UtcNow;
            loan.AssignTo(candidate.EmployeeId, now);
            AddAssignmentRecord(
                assignmentId,
                loan.Id,
                previousAssigneeId,
                candidate.EmployeeId,
                isAutomatic: false,
                actor.Id,
                request.Reason.Trim(),
                now);
            AddAudit(actor.Id, "LoanReassigned", loan.Id, "Loan manually reassigned with a team lead reason.");

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return LoanActionResult<AssignmentDecisionResponse>.Success(
                MapAssignmentResponse(loan, assigned: true, assignmentId, candidate, workload, "Loan reassigned manually."));
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return LoanActionResult<AssignmentDecisionResponse>.Conflict(
                "The loan was updated by another request. Refresh and retry.");
        }
    }

    public async Task<LoanActionResult<PriorityUpdateResponse>> UpdatePriorityAsync(
        Guid loanId,
        PriorityUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var actor = await RequireTeamLeadAsync(cancellationToken);
        if (actor is null)
        {
            return LoanActionResult<PriorityUpdateResponse>.Forbidden("Only team leads can update business priority.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return LoanActionResult<PriorityUpdateResponse>.ValidationFailed(
                new Dictionary<string, string[]> { [nameof(request.Reason)] = ["Priority update reason is required."] });
        }

        var loan = await _dbContext.LoanApplications.SingleOrDefaultAsync(item => item.Id == loanId, cancellationToken);
        if (loan is null)
        {
            return LoanActionResult<PriorityUpdateResponse>.NotFound();
        }

        if (!TryApplyRowVersion(loan, request.RowVersion, out var rowVersionErrors))
        {
            return LoanActionResult<PriorityUpdateResponse>.ValidationFailed(rowVersionErrors);
        }

        try
        {
            loan.SetBusinessPriority(request.BusinessPriority, DateTime.UtcNow);
            AddAudit(actor.Id, "LoanPriorityUpdated", loan.Id, "Loan business priority updated with a team lead reason.");
            await _dbContext.SaveChangesAsync(cancellationToken);

            var priorityScore = await CalculatePriorityScoreAsync(loan, cancellationToken);
            return LoanActionResult<PriorityUpdateResponse>.Success(new PriorityUpdateResponse(
                loan.Id,
                loan.LoanNumber.Value,
                loan.BusinessPriority,
                priorityScore,
                GetRowVersion(loan)));
        }
        catch (DbUpdateConcurrencyException)
        {
            return LoanActionResult<PriorityUpdateResponse>.Conflict(
                "The loan was updated by another request. Refresh and retry.");
        }
    }

    private async Task<ApplicationUser?> RequireTeamLeadAsync(CancellationToken cancellationToken)
    {
        var current = _currentUser.User;
        if (current is null || current.Role != MortgageFlowRoles.TeamLead)
        {
            return null;
        }

        return await _dbContext.Users.SingleOrDefaultAsync(user => user.Id == current.UserId, cancellationToken);
    }

    private bool TryCreateAssignmentContext(LoanApplication loan, Guid teamId, out LoanAssignmentContext context)
    {
        if (!AssignmentConstants.TryGetRouting(loan.Status, _options, out var role, out var skill))
        {
            context = default!;
            return false;
        }

        context = new LoanAssignmentContext(
            loan.Id,
            loan.LoanNumber.Value,
            loan.Status,
            teamId,
            role,
            skill,
            _priorityCalculator.Calculate(new LoanPriorityInput(
                loan.BusinessPriority,
                loan.Status,
                loan.CreatedUtc,
                loan.SubmittedUtc,
                EarliestDueUtc: null,
                DateTime.UtcNow)));
        return true;
    }

    private async Task<IReadOnlyCollection<EmployeeAssignmentCandidate>> LoadCandidatesAsync(
        LoanAssignmentContext context,
        CancellationToken cancellationToken)
    {
        var users = await (
            from user in _dbContext.Users.AsNoTracking()
            join userRole in _dbContext.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
            join role in _dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where role.Name == context.RequiredRole && user.TeamId == context.TeamId
            select new { User = user, Role = role.Name! })
            .ToListAsync(cancellationToken);

        var userIds = users.Select(item => item.User.Id).ToList();
        var skills = await _dbContext.EmployeeSkills.AsNoTracking()
            .Where(skill => userIds.Contains(skill.UserId))
            .GroupBy(skill => skill.UserId)
            .Select(group => new { UserId = group.Key, Tags = group.Select(skill => skill.SkillTag).ToList() })
            .ToDictionaryAsync(item => item.UserId, item => (IReadOnlyCollection<string>)item.Tags, cancellationToken);

        var activeLoanCounts = await _dbContext.LoanApplications.AsNoTracking()
            .Where(loan => loan.AssigneeId != null && userIds.Contains(loan.AssigneeId.Value))
            .Where(loan => loan.Status == LoanStatus.Submitted
                || loan.Status == LoanStatus.Processing
                || loan.Status == LoanStatus.Underwriting
                || loan.Status == LoanStatus.MoreInformationRequired)
            .GroupBy(loan => loan.AssigneeId!.Value)
            .Select(group => new { AssigneeId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.AssigneeId, item => item.Count, cancellationToken);

        var openTasks = await _dbContext.WorkflowTasks.AsNoTracking()
            .Where(task => !task.IsComplete && userIds.Contains(task.AssigneeId))
            .Select(task => new { task.AssigneeId, task.Priority })
            .ToListAsync(cancellationToken);
        var taskWeights = openTasks
            .GroupBy(task => task.AssigneeId)
            .ToDictionary(group => group.Key, group => group.Sum(task => TaskWeight(task.Priority)));

        return users.Select(item => new EmployeeAssignmentCandidate(
                item.User.Id,
                item.User.FullName,
                item.Role,
                item.User.TeamId,
                item.User.IsActive,
                item.User.IsAvailable,
                item.User.CapacityPoints,
                skills.GetValueOrDefault(item.User.Id, []),
                taskWeights.GetValueOrDefault(item.User.Id),
                activeLoanCounts.GetValueOrDefault(item.User.Id)))
            .ToList();
    }

    private async Task<int> CalculatePriorityScoreAsync(LoanApplication loan, CancellationToken cancellationToken)
    {
        var earliestDue = await _dbContext.WorkflowTasks.AsNoTracking()
            .Where(task => task.LoanApplicationId == loan.Id && !task.IsComplete)
            .OrderBy(task => task.DueUtc)
            .Select(task => (DateTime?)task.DueUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return _priorityCalculator.Calculate(new LoanPriorityInput(
            loan.BusinessPriority,
            loan.Status,
            loan.CreatedUtc,
            loan.SubmittedUtc,
            earliestDue,
            DateTime.UtcNow));
    }

    private static int TaskWeight(BusinessPriority priority)
    {
        return priority switch
        {
            BusinessPriority.High => 3,
            BusinessPriority.Urgent => 5,
            _ => 2
        };
    }

    private bool TryApplyRowVersion(
        LoanApplication loan,
        string rowVersion,
        out Dictionary<string, string[]> validationErrors)
    {
        validationErrors = [];

        if (string.IsNullOrWhiteSpace(rowVersion))
        {
            validationErrors[nameof(rowVersion)] = ["Row version is required."];
            return false;
        }

        try
        {
            _dbContext.Entry(loan).Property<byte[]>("RowVersion").OriginalValue = Convert.FromBase64String(rowVersion);
            return true;
        }
        catch (FormatException)
        {
            validationErrors[nameof(rowVersion)] = ["Row version must be a valid base64 string."];
            return false;
        }
    }

    private void AddAssignmentRecord(
        Guid assignmentId,
        Guid loanId,
        Guid? previousAssigneeId,
        Guid newAssigneeId,
        bool isAutomatic,
        Guid actorId,
        string reason,
        DateTime assignedUtc)
    {
        _dbContext.LoanAssignments.Add(new LoanAssignmentRecord
        {
            Id = assignmentId,
            LoanApplicationId = loanId,
            PreviousAssigneeId = previousAssigneeId,
            NewAssigneeId = newAssigneeId,
            IsAutomatic = isAutomatic,
            ActorId = actorId,
            Reason = reason,
            AssignedUtc = assignedUtc
        });
    }

    private void AddAudit(Guid actorId, string action, Guid loanId, string summary)
    {
        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            Action = action,
            EntityType = nameof(LoanApplication),
            EntityId = loanId.ToString(),
            Summary = summary,
            CorrelationId = _correlationContext.CorrelationId,
            CreatedUtc = DateTime.UtcNow
        });
    }

    private AssignmentDecisionResponse MapAssignmentResponse(
        LoanApplication loan,
        bool assigned,
        Guid? assignmentId,
        EmployeeAssignmentCandidate? candidate,
        EmployeeWorkload? workload,
        string result)
    {
        return new AssignmentDecisionResponse(
            loan.Id,
            loan.LoanNumber.Value,
            assigned,
            assignmentId,
            candidate?.EmployeeId ?? loan.AssigneeId,
            candidate?.FullName,
            result,
            _priorityCalculator.Calculate(new LoanPriorityInput(
                loan.BusinessPriority,
                loan.Status,
                loan.CreatedUtc,
                loan.SubmittedUtc,
                EarliestDueUtc: null,
                DateTime.UtcNow)),
            workload?.NormalizedLoad,
            GetRowVersion(loan));
    }

    private string GetRowVersion(LoanApplication loan)
    {
        var rowVersion = _dbContext.Entry(loan).Property<byte[]>("RowVersion").CurrentValue;
        return Convert.ToBase64String(rowVersion ?? []);
    }
}
