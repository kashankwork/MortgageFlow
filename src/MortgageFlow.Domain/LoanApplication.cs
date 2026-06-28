namespace MortgageFlow.Domain;

public sealed class LoanApplication
{
    private readonly List<LoanStatusChange> _statusHistory = [];

    private LoanApplication()
    {
        LoanNumber = LoanNumber.Create("MF-000000");
        RequestedAmount = Money.Usd(0);
    }

    private LoanApplication(
        LoanNumber loanNumber,
        Guid brokerId,
        Money requestedAmount,
        DateTime createdUtc)
    {
        Id = Guid.NewGuid();
        LoanNumber = loanNumber;
        BrokerId = brokerId;
        RequestedAmount = requestedAmount;
        Status = LoanStatus.Draft;
        BusinessPriority = BusinessPriority.Normal;
        CreatedUtc = RequireUtc(createdUtc, nameof(createdUtc));
        UpdatedUtc = CreatedUtc;
    }

    public Guid Id { get; private set; }

    public LoanNumber LoanNumber { get; private set; }

    public Guid BrokerId { get; private set; }

    public Guid? AssigneeId { get; private set; }

    public LoanStatus Status { get; private set; }

    public BusinessPriority BusinessPriority { get; private set; }

    public Money RequestedAmount { get; private set; }

    public LoanPurpose? LoanPurpose { get; private set; }

    public decimal? InterestRatePercent { get; private set; }

    public int? TermMonths { get; private set; }

    public Borrower? Borrower { get; private set; }

    public Property? Property { get; private set; }

    public DateTime CreatedUtc { get; private set; }

    public DateTime UpdatedUtc { get; private set; }

    public DateTime? SubmittedUtc { get; private set; }

    public IReadOnlyCollection<LoanStatusChange> StatusHistory => _statusHistory.AsReadOnly();

    public static LoanApplication CreateDraft(
        LoanNumber loanNumber,
        Guid brokerId,
        Money requestedAmount,
        DateTime createdUtc)
    {
        if (brokerId == Guid.Empty)
        {
            throw new DomainValidationException("Broker id is required.");
        }

        if (requestedAmount.Amount <= 0)
        {
            throw new DomainValidationException("Requested loan amount must be greater than zero.");
        }

        return new LoanApplication(loanNumber, brokerId, requestedAmount, createdUtc);
    }

    public void AddBorrower(Borrower borrower, DateTime changedUtc)
    {
        EnsureDraftIsEditable();
        var utc = RequireChronologicalUtc(changedUtc, nameof(changedUtc));
        Borrower = borrower;
        UpdatedUtc = utc;
    }

    public void AddProperty(Property property, DateTime changedUtc)
    {
        EnsureDraftIsEditable();
        var utc = RequireChronologicalUtc(changedUtc, nameof(changedUtc));
        Property = property;
        UpdatedUtc = utc;
    }

    public void UpdateLoanTerms(
        Money requestedAmount,
        LoanPurpose loanPurpose,
        decimal interestRatePercent,
        int termMonths,
        DateTime changedUtc)
    {
        EnsureDraftIsEditable();

        if (requestedAmount.Amount <= 0)
        {
            throw new DomainValidationException("Requested loan amount must be greater than zero.");
        }

        if (interestRatePercent <= 0 || interestRatePercent > 25)
        {
            throw new DomainValidationException("Interest rate must be greater than zero and no more than 25 percent.");
        }

        if (termMonths is < 60 or > 480)
        {
            throw new DomainValidationException("Loan term must be between 60 and 480 months.");
        }

        var utc = RequireChronologicalUtc(changedUtc, nameof(changedUtc));
        RequestedAmount = requestedAmount;
        LoanPurpose = loanPurpose;
        InterestRatePercent = decimal.Round(interestRatePercent, 3);
        TermMonths = termMonths;
        UpdatedUtc = utc;
    }

    public void AssignTo(Guid assigneeId, DateTime changedUtc)
    {
        if (assigneeId == Guid.Empty)
        {
            throw new DomainValidationException("Assignee id is required.");
        }

        var utc = RequireChronologicalUtc(changedUtc, nameof(changedUtc));
        AssigneeId = assigneeId;
        UpdatedUtc = utc;
    }

    public void SetBusinessPriority(BusinessPriority businessPriority, DateTime changedUtc)
    {
        var utc = RequireChronologicalUtc(changedUtc, nameof(changedUtc));
        BusinessPriority = businessPriority;
        UpdatedUtc = utc;
    }

    public void TransitionTo(LoanStatus nextStatus, Guid actorId, string? reason, DateTime changedUtc)
    {
        if (actorId == Guid.Empty)
        {
            throw new DomainValidationException("Actor id is required.");
        }

        var utc = RequireChronologicalUtc(changedUtc, nameof(changedUtc));
        EnsureTransitionIsAllowed(Status, nextStatus, reason);

        if (Status == LoanStatus.Draft && nextStatus == LoanStatus.Submitted)
        {
            EnsureReadyForSubmission();
            SubmittedUtc = utc;
        }

        var previous = Status;
        Status = nextStatus;
        UpdatedUtc = utc;
        _statusHistory.Add(new LoanStatusChange(previous, nextStatus, actorId, NormalizeOptionalReason(reason), utc));
    }

    private void EnsureDraftIsEditable()
    {
        if (Status != LoanStatus.Draft)
        {
            throw new DomainValidationException("Only draft loans can be edited.");
        }
    }

    private void EnsureReadyForSubmission()
    {
        if (Borrower is null || Property is null || LoanPurpose is null || InterestRatePercent is null || TermMonths is null)
        {
            throw new DomainValidationException("A loan must include borrower, property, and loan terms before submission.");
        }

        if (RequestedAmount.Amount > Property.EstimatedValue.Amount)
        {
            throw new DomainValidationException("Requested loan amount cannot exceed the estimated property value.");
        }
    }

    private static void EnsureTransitionIsAllowed(LoanStatus current, LoanStatus next, string? reason)
    {
        if (current is LoanStatus.Approved or LoanStatus.Rejected)
        {
            throw new DomainValidationException("Approved and rejected loans are terminal.");
        }

        var allowed = current switch
        {
            LoanStatus.Draft => next == LoanStatus.Submitted,
            LoanStatus.Submitted => next == LoanStatus.Processing,
            LoanStatus.Processing => next is LoanStatus.Underwriting or LoanStatus.MoreInformationRequired,
            LoanStatus.MoreInformationRequired => next == LoanStatus.Processing,
            LoanStatus.Underwriting => next is LoanStatus.Approved or LoanStatus.Rejected or LoanStatus.MoreInformationRequired,
            _ => false
        };

        if (!allowed)
        {
            throw new DomainValidationException($"Cannot transition loan from {current} to {next}.");
        }

        if (RequiresReason(current, next) && string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainValidationException($"Transition from {current} to {next} requires a reason.");
        }
    }

    private static bool RequiresReason(LoanStatus current, LoanStatus next)
    {
        return next is LoanStatus.Approved or LoanStatus.Rejected or LoanStatus.MoreInformationRequired
            || current == LoanStatus.Processing && next == LoanStatus.Underwriting;
    }

    private DateTime RequireChronologicalUtc(DateTime value, string parameterName)
    {
        var utc = RequireUtc(value, parameterName);
        if (utc < UpdatedUtc)
        {
            throw new DomainValidationException($"{parameterName} cannot be earlier than the last loan change.");
        }

        return utc;
    }

    private static DateTime RequireUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new DomainValidationException($"{parameterName} must be a UTC timestamp.");
        }

        return value;
    }

    private static string? NormalizeOptionalReason(string? reason)
    {
        return string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }
}
