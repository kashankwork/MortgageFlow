using Microsoft.EntityFrameworkCore;
using MortgageFlow.Application.Assignments;
using MortgageFlow.Application.Authentication;
using MortgageFlow.Application.Loans;
using MortgageFlow.Application.Users;
using MortgageFlow.Domain;
using MortgageFlow.Infrastructure.Persistence;

namespace MortgageFlow.Infrastructure.Assignments;

public sealed class EfQueueService : IQueueService
{
    private const int MaxPageSize = 50;

    private readonly MortgageFlowDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ILoanPriorityCalculator _priorityCalculator;

    public EfQueueService(
        MortgageFlowDbContext dbContext,
        ICurrentUser currentUser,
        ILoanPriorityCalculator priorityCalculator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _priorityCalculator = priorityCalculator;
    }

    public async Task<LoanActionResult<PagedResult<QueueItemResponse>>> GetMyQueueAsync(
        int page,
        int pageSize,
        LoanStatus? status,
        CancellationToken cancellationToken)
    {
        var user = _currentUser.User;
        if (user is null)
        {
            return LoanActionResult<PagedResult<QueueItemResponse>>.Forbidden("Authentication is required.");
        }

        IQueryable<LoanApplication> query = _dbContext.LoanApplications.AsNoTracking()
            .Where(loan => loan.AssigneeId == user.UserId)
            .Where(loan => loan.Status == LoanStatus.Submitted
                || loan.Status == LoanStatus.Processing
                || loan.Status == LoanStatus.Underwriting
                || loan.Status == LoanStatus.MoreInformationRequired);

        query = user.Role switch
        {
            MortgageFlowRoles.Processor => query.Where(loan => loan.Status == LoanStatus.Submitted
                || loan.Status == LoanStatus.Processing
                || loan.Status == LoanStatus.MoreInformationRequired),
            MortgageFlowRoles.Underwriter => query.Where(loan => loan.Status == LoanStatus.Underwriting),
            _ => query.Where(_ => false)
        };

        return await CreateQueuePageAsync(query, page, pageSize, status, cancellationToken);
    }

    public async Task<LoanActionResult<PagedResult<QueueItemResponse>>> GetTeamQueueAsync(
        int page,
        int pageSize,
        LoanStatus? status,
        CancellationToken cancellationToken)
    {
        var user = _currentUser.User;
        if (user is null)
        {
            return LoanActionResult<PagedResult<QueueItemResponse>>.Forbidden("Authentication is required.");
        }

        if (user.Role != MortgageFlowRoles.TeamLead)
        {
            return LoanActionResult<PagedResult<QueueItemResponse>>.Forbidden("Only team leads can view the team queue.");
        }

        var query = _dbContext.LoanApplications.AsNoTracking()
            .Where(loan => loan.Status == LoanStatus.Submitted
                || loan.Status == LoanStatus.Processing
                || loan.Status == LoanStatus.Underwriting
                || loan.Status == LoanStatus.MoreInformationRequired);

        return await CreateQueuePageAsync(query, page, pageSize, status, cancellationToken);
    }

    private async Task<LoanActionResult<PagedResult<QueueItemResponse>>> CreateQueuePageAsync(
        IQueryable<LoanApplication> query,
        int page,
        int pageSize,
        LoanStatus? status,
        CancellationToken cancellationToken)
    {
        if (status is not null)
        {
            query = query.Where(loan => loan.Status == status);
        }

        var loans = await query.ToListAsync(cancellationToken);
        var loanIds = loans.Select(loan => loan.Id).ToList();
        var assigneeIds = loans
            .Where(loan => loan.AssigneeId is not null)
            .Select(loan => loan.AssigneeId!.Value)
            .Distinct()
            .ToList();

        var dueDates = await _dbContext.WorkflowTasks.AsNoTracking()
            .Where(task => loanIds.Contains(task.LoanApplicationId) && !task.IsComplete)
            .GroupBy(task => task.LoanApplicationId)
            .Select(group => new { LoanId = group.Key, EarliestDueUtc = group.Min(task => task.DueUtc) })
            .ToDictionaryAsync(item => item.LoanId, item => (DateTime?)item.EarliestDueUtc, cancellationToken);

        var assigneeNames = await _dbContext.Users.AsNoTracking()
            .Where(user => assigneeIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.FullName, cancellationToken);

        var now = DateTime.UtcNow;
        var items = loans
            .Select(loan =>
            {
                var earliestDue = dueDates.GetValueOrDefault(loan.Id);
                var priorityScore = _priorityCalculator.Calculate(new LoanPriorityInput(
                    loan.BusinessPriority,
                    loan.Status,
                    loan.CreatedUtc,
                    loan.SubmittedUtc,
                    earliestDue,
                    now));

                return new QueueItemResponse(
                    loan.Id,
                    loan.LoanNumber.Value,
                    loan.Status,
                    loan.BusinessPriority,
                    priorityScore,
                    earliestDue,
                    loan.AssigneeId,
                    loan.AssigneeId is null ? null : assigneeNames.GetValueOrDefault(loan.AssigneeId.Value),
                    loan.CreatedUtc,
                    loan.UpdatedUtc,
                    loan.SubmittedUtc);
            })
            .OrderByDescending(item => item.PriorityScore)
            .ThenBy(item => item.SubmittedUtc ?? item.CreatedUtc)
            .ThenBy(item => item.LoanNumber, StringComparer.Ordinal)
            .ToList();

        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var pageItems = items
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToList();

        return LoanActionResult<PagedResult<QueueItemResponse>>.Success(
            new PagedResult<QueueItemResponse>(pageItems, normalizedPage, normalizedPageSize, items.Count));
    }
}
