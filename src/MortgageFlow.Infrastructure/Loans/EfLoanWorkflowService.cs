using Microsoft.EntityFrameworkCore;
using MortgageFlow.Application.Authentication;
using MortgageFlow.Application.Diagnostics;
using MortgageFlow.Application.Loans;
using MortgageFlow.Application.Users;
using MortgageFlow.Domain;
using MortgageFlow.Infrastructure.Persistence;
using MortgageFlow.Infrastructure.Persistence.Entities;

namespace MortgageFlow.Infrastructure.Loans;

public sealed class EfLoanWorkflowService : ILoanWorkflowService
{
    private const int MaxPageSize = 50;

    private readonly MortgageFlowDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ICorrelationContext _correlationContext;

    public EfLoanWorkflowService(
        MortgageFlowDbContext dbContext,
        ICurrentUser currentUser,
        ICorrelationContext correlationContext)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _correlationContext = correlationContext;
    }

    public async Task<LoanActionResult<LoanDetailResponse>> CreateDraftAsync(
        CreateLoanRequest request,
        CancellationToken cancellationToken)
    {
        var user = _currentUser.User;
        if (user is null || user.Role != MortgageFlowRoles.Broker)
        {
            return LoanActionResult<LoanDetailResponse>.Forbidden("Only brokers can create loan drafts.");
        }

        var validationErrors = ValidateCreateOrUpdate(request.RequestedAmount, request.Borrower, request.Property);
        if (validationErrors.Count > 0)
        {
            return LoanActionResult<LoanDetailResponse>.ValidationFailed(validationErrors);
        }

        var now = DateTime.UtcNow;
        var loan = LoanApplication.CreateDraft(
            await CreateNextLoanNumberAsync(cancellationToken),
            user.UserId,
            Money.Usd(request.RequestedAmount),
            now);

        var applyResult = ApplyDraftDetails(loan, request, now.AddMilliseconds(1));
        if (applyResult.Count > 0)
        {
            return LoanActionResult<LoanDetailResponse>.ValidationFailed(applyResult);
        }

        _dbContext.LoanApplications.Add(loan);
        AddAudit(user.UserId, "LoanCreated", loan.Id, "Loan draft created.");

        await _dbContext.SaveChangesAsync(cancellationToken);

        return LoanActionResult<LoanDetailResponse>.Success(MapDetail(loan));
    }

    public async Task<LoanActionResult<PagedResult<LoanListItemResponse>>> ListAsync(
        int page,
        int pageSize,
        string? search,
        LoanStatus? status,
        CancellationToken cancellationToken)
    {
        var user = _currentUser.User;
        if (user is null)
        {
            return LoanActionResult<PagedResult<LoanListItemResponse>>.Forbidden("Authentication is required.");
        }

        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var query = ApplyVisibility(_dbContext.LoanApplications.AsNoTracking(), user);

        if (status is not null)
        {
            query = query.Where(loan => loan.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            if (TryCreateLoanNumber(term, out var exactLoanNumber))
            {
                query = query.Where(loan =>
                    loan.LoanNumber == exactLoanNumber
                    || loan.Borrower != null && loan.Borrower.FullName.Contains(term));
            }
            else
            {
                query = query.Where(loan => loan.Borrower != null && loan.Borrower.FullName.Contains(term));
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(loan => loan.SubmittedUtc ?? loan.UpdatedUtc)
            .ThenBy(loan => loan.LoanNumber)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(loan => new LoanListItemResponse(
                loan.Id,
                loan.LoanNumber.Value,
                loan.Status,
                loan.RequestedAmount.Amount,
                loan.Borrower == null ? null : loan.Borrower.FullName,
                loan.CreatedUtc,
                loan.UpdatedUtc,
                loan.SubmittedUtc))
            .ToListAsync(cancellationToken);

        return LoanActionResult<PagedResult<LoanListItemResponse>>.Success(
            new PagedResult<LoanListItemResponse>(items, normalizedPage, normalizedPageSize, totalCount));
    }

    public async Task<LoanActionResult<LoanDetailResponse>> GetDetailAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = _currentUser.User;
        if (user is null)
        {
            return LoanActionResult<LoanDetailResponse>.Forbidden("Authentication is required.");
        }

        var loan = await FindLoanAsync(id, includeHistory: false, cancellationToken);
        if (loan is null || !CanView(user, loan))
        {
            return LoanActionResult<LoanDetailResponse>.NotFound();
        }

        return LoanActionResult<LoanDetailResponse>.Success(MapDetail(loan));
    }

    public async Task<LoanActionResult<LoanDetailResponse>> UpdateDraftAsync(
        Guid id,
        UpdateLoanRequest request,
        CancellationToken cancellationToken)
    {
        var user = _currentUser.User;
        if (user is null)
        {
            return LoanActionResult<LoanDetailResponse>.Forbidden("Authentication is required.");
        }

        var loan = await FindLoanAsync(id, includeHistory: false, cancellationToken);
        if (loan is null || !CanView(user, loan))
        {
            return LoanActionResult<LoanDetailResponse>.NotFound();
        }

        if (user.Role != MortgageFlowRoles.Broker || loan.BrokerId != user.UserId || loan.Status != LoanStatus.Draft)
        {
            return LoanActionResult<LoanDetailResponse>.Forbidden("Only the owning broker can update a draft loan.");
        }

        if (!TryApplyRowVersion(loan, request.RowVersion, out var rowVersionErrors))
        {
            return LoanActionResult<LoanDetailResponse>.ValidationFailed(rowVersionErrors);
        }

        var validationErrors = ValidateCreateOrUpdate(request.RequestedAmount, request.Borrower, request.Property);
        if (validationErrors.Count > 0)
        {
            return LoanActionResult<LoanDetailResponse>.ValidationFailed(validationErrors);
        }

        var applyResult = ApplyDraftDetails(loan, request, DateTime.UtcNow);
        if (applyResult.Count > 0)
        {
            return LoanActionResult<LoanDetailResponse>.ValidationFailed(applyResult);
        }

        AddAudit(user.UserId, "LoanUpdated", loan.Id, "Loan draft updated.");

        var saved = await TrySaveChangesAsync(cancellationToken);
        return saved
            ? LoanActionResult<LoanDetailResponse>.Success(MapDetail(loan))
            : LoanActionResult<LoanDetailResponse>.Conflict("The loan was updated by another request. Refresh and retry.");
    }

    public async Task<LoanActionResult<LoanDetailResponse>> TransitionAsync(
        Guid id,
        TransitionLoanRequest request,
        CancellationToken cancellationToken)
    {
        var user = _currentUser.User;
        if (user is null)
        {
            return LoanActionResult<LoanDetailResponse>.Forbidden("Authentication is required.");
        }

        var loan = await FindLoanAsync(id, includeHistory: true, cancellationToken);
        if (loan is null || !CanView(user, loan))
        {
            return LoanActionResult<LoanDetailResponse>.NotFound();
        }

        if (!CanTransition(user, loan, request.NextStatus))
        {
            return LoanActionResult<LoanDetailResponse>.Forbidden("The current role cannot perform this transition.");
        }

        if (!TryApplyRowVersion(loan, request.RowVersion, out var rowVersionErrors))
        {
            return LoanActionResult<LoanDetailResponse>.ValidationFailed(rowVersionErrors);
        }

        if (loan.Status == LoanStatus.Draft && request.NextStatus == LoanStatus.Submitted)
        {
            var submissionErrors = ValidateReadyForSubmission(loan);
            if (submissionErrors.Count > 0)
            {
                return LoanActionResult<LoanDetailResponse>.ValidationFailed(submissionErrors);
            }
        }

        var previousStatus = loan.Status;
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            loan.TransitionTo(request.NextStatus, user.UserId, request.Reason, DateTime.UtcNow);
            AddAudit(
                user.UserId,
                "LoanTransitioned",
                loan.Id,
                $"Loan transitioned from {previousStatus} to {request.NextStatus}.");

            var saved = await TrySaveChangesAsync(cancellationToken);
            if (!saved)
            {
                return LoanActionResult<LoanDetailResponse>.Conflict(
                    "The loan was updated by another request. Refresh and retry.");
            }

            await transaction.CommitAsync(cancellationToken);
            return LoanActionResult<LoanDetailResponse>.Success(MapDetail(loan));
        }
        catch (DomainValidationException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return LoanActionResult<LoanDetailResponse>.InvalidTransition(exception.Message);
        }
    }

    public async Task<LoanActionResult<IReadOnlyCollection<LoanStatusHistoryResponse>>> GetHistoryAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = _currentUser.User;
        if (user is null)
        {
            return LoanActionResult<IReadOnlyCollection<LoanStatusHistoryResponse>>.Forbidden("Authentication is required.");
        }

        var loan = await FindLoanAsync(id, includeHistory: true, cancellationToken);
        if (loan is null || !CanView(user, loan))
        {
            return LoanActionResult<IReadOnlyCollection<LoanStatusHistoryResponse>>.NotFound();
        }

        var history = loan.StatusHistory
            .OrderBy(item => item.ChangedUtc)
            .Select(item => new LoanStatusHistoryResponse(
                item.PreviousStatus,
                item.NewStatus,
                item.ActorId,
                item.Reason,
                item.ChangedUtc))
            .ToList();

        return LoanActionResult<IReadOnlyCollection<LoanStatusHistoryResponse>>.Success(history);
    }

    private async Task<LoanApplication?> FindLoanAsync(
        Guid id,
        bool includeHistory,
        CancellationToken cancellationToken)
    {
        IQueryable<LoanApplication> query = _dbContext.LoanApplications;
        if (includeHistory)
        {
            query = query.Include(loan => loan.StatusHistory);
        }

        return await query.SingleOrDefaultAsync(loan => loan.Id == id, cancellationToken);
    }

    private IQueryable<LoanApplication> ApplyVisibility(IQueryable<LoanApplication> query, CurrentUser user)
    {
        return user.Role switch
        {
            MortgageFlowRoles.Broker => query.Where(loan => loan.BrokerId == user.UserId),
            MortgageFlowRoles.Processor => query.Where(loan =>
                loan.AssigneeId == user.UserId
                && (loan.Status == LoanStatus.Submitted
                    || loan.Status == LoanStatus.Processing
                    || loan.Status == LoanStatus.MoreInformationRequired)),
            MortgageFlowRoles.Underwriter => query.Where(loan =>
                loan.AssigneeId == user.UserId && loan.Status == LoanStatus.Underwriting),
            MortgageFlowRoles.TeamLead => query,
            _ => query.Where(_ => false)
        };
    }

    private static bool CanView(CurrentUser user, LoanApplication loan)
    {
        return user.Role switch
        {
            MortgageFlowRoles.Broker => loan.BrokerId == user.UserId,
            MortgageFlowRoles.Processor => loan.AssigneeId == user.UserId
                && loan.Status is LoanStatus.Submitted or LoanStatus.Processing or LoanStatus.MoreInformationRequired,
            MortgageFlowRoles.Underwriter => loan.AssigneeId == user.UserId && loan.Status == LoanStatus.Underwriting,
            MortgageFlowRoles.TeamLead => true,
            _ => false
        };
    }

    private static bool CanTransition(CurrentUser user, LoanApplication loan, LoanStatus nextStatus)
    {
        return user.Role switch
        {
            MortgageFlowRoles.Broker => loan.BrokerId == user.UserId
                && loan.Status == LoanStatus.Draft
                && nextStatus == LoanStatus.Submitted,
            MortgageFlowRoles.Processor => loan.AssigneeId == user.UserId
                && loan.Status is LoanStatus.Submitted or LoanStatus.Processing
                && nextStatus is LoanStatus.Processing or LoanStatus.Underwriting or LoanStatus.MoreInformationRequired,
            MortgageFlowRoles.Underwriter => loan.AssigneeId == user.UserId
                && loan.Status == LoanStatus.Underwriting
                && nextStatus is LoanStatus.Approved or LoanStatus.Rejected or LoanStatus.MoreInformationRequired,
            _ => false
        };
    }

    private static Dictionary<string, string[]> ValidateCreateOrUpdate(
        decimal requestedAmount,
        BorrowerDto? borrower,
        PropertyDto? property)
    {
        var errors = new Dictionary<string, string[]>();

        if (requestedAmount <= 0)
        {
            errors[nameof(CreateLoanRequest.RequestedAmount)] = ["Requested amount must be greater than zero."];
        }

        if (borrower is not null && borrower.AnnualIncome < 0)
        {
            errors["Borrower.AnnualIncome"] = ["Annual income cannot be negative."];
        }

        if (property is not null && property.EstimatedValue <= 0)
        {
            errors["Property.EstimatedValue"] = ["Estimated property value must be greater than zero."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateReadyForSubmission(LoanApplication loan)
    {
        var errors = new Dictionary<string, string[]>();

        if (loan.Borrower is null)
        {
            errors[nameof(LoanApplication.Borrower)] = ["Borrower details are required before submission."];
        }

        if (loan.Property is null)
        {
            errors[nameof(LoanApplication.Property)] = ["Property details are required before submission."];
        }

        if (loan.LoanPurpose is null)
        {
            errors[nameof(LoanApplication.LoanPurpose)] = ["Loan purpose is required before submission."];
        }

        if (loan.InterestRatePercent is null)
        {
            errors[nameof(LoanApplication.InterestRatePercent)] = ["Interest rate is required before submission."];
        }

        if (loan.TermMonths is null)
        {
            errors[nameof(LoanApplication.TermMonths)] = ["Loan term is required before submission."];
        }

        if (loan.Property is not null && loan.RequestedAmount.Amount > loan.Property.EstimatedValue.Amount)
        {
            errors[nameof(LoanApplication.RequestedAmount)] =
                ["Requested loan amount cannot exceed the estimated property value."];
        }

        return errors;
    }

    private Dictionary<string, string[]> ApplyDraftDetails(
        LoanApplication loan,
        CreateLoanRequest request,
        DateTime changedUtc)
    {
        return ApplyDraftDetails(
            loan,
            request.RequestedAmount,
            request.LoanPurpose,
            request.InterestRatePercent,
            request.TermMonths,
            request.Borrower,
            request.Property,
            changedUtc);
    }

    private Dictionary<string, string[]> ApplyDraftDetails(
        LoanApplication loan,
        UpdateLoanRequest request,
        DateTime changedUtc)
    {
        return ApplyDraftDetails(
            loan,
            request.RequestedAmount,
            request.LoanPurpose,
            request.InterestRatePercent,
            request.TermMonths,
            request.Borrower,
            request.Property,
            changedUtc);
    }

    private static Dictionary<string, string[]> ApplyDraftDetails(
        LoanApplication loan,
        decimal requestedAmount,
        LoanPurpose? loanPurpose,
        decimal? interestRatePercent,
        int? termMonths,
        BorrowerDto? borrower,
        PropertyDto? property,
        DateTime changedUtc)
    {
        var errors = new Dictionary<string, string[]>();

        try
        {
            if (borrower is not null)
            {
                loan.AddBorrower(
                    Borrower.Create(borrower.FullName, borrower.Email, Money.Usd(borrower.AnnualIncome)),
                    changedUtc);
                changedUtc = changedUtc.AddMilliseconds(1);
            }

            if (property is not null)
            {
                loan.AddProperty(
                    Property.Create(
                        property.StreetAddress,
                        property.City,
                        property.State,
                        property.PostalCode,
                        Money.Usd(property.EstimatedValue),
                        property.OccupancyType),
                    changedUtc);
                changedUtc = changedUtc.AddMilliseconds(1);
            }

            if (loanPurpose is not null && interestRatePercent is not null && termMonths is not null)
            {
                loan.UpdateLoanTerms(
                    Money.Usd(requestedAmount),
                    loanPurpose.Value,
                    interestRatePercent.Value,
                    termMonths.Value,
                    changedUtc);
            }
        }
        catch (DomainValidationException exception)
        {
            errors["Loan"] = [exception.Message];
        }

        return errors;
    }

    private bool TryApplyRowVersion(
        LoanApplication loan,
        string rowVersion,
        out Dictionary<string, string[]> validationErrors)
    {
        validationErrors = new Dictionary<string, string[]>();

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

    private async Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    private async Task<LoanNumber> CreateNextLoanNumberAsync(CancellationToken cancellationToken)
    {
        var nextSequence = await _dbContext.LoanApplications.CountAsync(cancellationToken) + 1;

        while (true)
        {
            var candidate = LoanNumber.Create($"MF-{nextSequence:000000}");
            var exists = await _dbContext.LoanApplications
                .AnyAsync(
                    loan => loan.LoanNumber == candidate,
                    cancellationToken);
            if (!exists)
            {
                return candidate;
            }

            nextSequence++;
        }
    }

    private static bool TryCreateLoanNumber(string value, out LoanNumber? loanNumber)
    {
        try
        {
            loanNumber = LoanNumber.Create(value);
            return true;
        }
        catch (DomainValidationException)
        {
            loanNumber = null;
            return false;
        }
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

    private LoanDetailResponse MapDetail(LoanApplication loan)
    {
        var rowVersion = _dbContext.Entry(loan).Property<byte[]>("RowVersion").CurrentValue;

        return new LoanDetailResponse(
            loan.Id,
            loan.LoanNumber.Value,
            loan.BrokerId,
            loan.AssigneeId,
            loan.Status,
            loan.BusinessPriority,
            loan.RequestedAmount.Amount,
            loan.LoanPurpose,
            loan.InterestRatePercent,
            loan.TermMonths,
            loan.Borrower is null
                ? null
                : new BorrowerDto(loan.Borrower.FullName, loan.Borrower.Email, loan.Borrower.AnnualIncome.Amount),
            loan.Property is null
                ? null
                : new PropertyDto(
                    loan.Property.StreetAddress,
                    loan.Property.City,
                    loan.Property.State,
                    loan.Property.PostalCode,
                    loan.Property.EstimatedValue.Amount,
                    loan.Property.OccupancyType),
            loan.CreatedUtc,
            loan.UpdatedUtc,
            loan.SubmittedUtc,
            Convert.ToBase64String(rowVersion ?? []));
    }
}
