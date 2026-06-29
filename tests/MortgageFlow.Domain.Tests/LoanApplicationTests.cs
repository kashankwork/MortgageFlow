using MortgageFlow.Domain;

namespace MortgageFlow.Domain.Tests;

public sealed class LoanApplicationTests
{
    private static readonly Guid BrokerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ActorId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTime Now = new(2026, 6, 23, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CreateDraft_WhenLoanNumberIsEmpty_RejectsIt()
    {
        Assert.Throws<DomainValidationException>(() => LoanNumber.Create(" "));
    }

    [Theory]
    [InlineData("123")]
    [InlineData("MF-ABCDEF")]
    [InlineData("LOAN-000001")]
    public void CreateDraft_WhenLoanNumberFormatIsInvalid_RejectsIt(string value)
    {
        Assert.Throws<DomainValidationException>(() => LoanNumber.Create(value));
    }

    [Fact]
    public void Usd_WhenAmountIsNegative_RejectsIt()
    {
        Assert.Throws<DomainValidationException>(() => Money.Usd(-1));
    }

    [Fact]
    public void Usd_WhenAmountHasMoreThanTwoDecimals_RoundsAndPreservesCurrency()
    {
        var money = Money.Usd(10.129m);

        Assert.Equal(10.13m, money.Amount);
        Assert.Equal("USD", money.Currency);
    }

    [Fact]
    public void TransitionToSubmitted_WhenDraftIsIncomplete_RejectsIt()
    {
        var loan = CreateDraft();

        var exception = Assert.Throws<DomainValidationException>(
            () => loan.TransitionTo(LoanStatus.Submitted, ActorId, null, Now.AddMinutes(1)));

        Assert.Contains("borrower, property, and loan terms", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(LoanStatus.Draft, loan.Status);
        Assert.Empty(loan.StatusHistory);
    }

    [Fact]
    public void TransitionToSubmitted_WhenDraftIsComplete_SucceedsAndRecordsHistory()
    {
        var loan = CreateCompleteDraft();

        loan.TransitionTo(LoanStatus.Submitted, ActorId, null, Now.AddMinutes(1));

        Assert.Equal(LoanStatus.Submitted, loan.Status);
        Assert.Equal(Now.AddMinutes(1), loan.SubmittedUtc);
        var history = Assert.Single(loan.StatusHistory);
        Assert.Equal(LoanStatus.Draft, history.PreviousStatus);
        Assert.Equal(LoanStatus.Submitted, history.NewStatus);
    }

    [Fact]
    public void AddBorrower_WhenTimestampPrecedesLastChange_RejectsWithoutMutation()
    {
        var loan = CreateDraft();
        var originalBorrower = Borrower.Create("Original Borrower", "original@example.test", Money.Usd(100_000));
        loan.AddBorrower(originalBorrower, Now.AddMinutes(1));

        var replacement = Borrower.Create("Replacement Borrower", "replacement@example.test", Money.Usd(110_000));

        Assert.Throws<DomainValidationException>(() => loan.AddBorrower(replacement, Now));

        Assert.Same(originalBorrower, loan.Borrower);
        Assert.Equal(Now.AddMinutes(1), loan.UpdatedUtc);
    }

    [Fact]
    public void TransitionToSubmitted_WhenTimestampPrecedesLastChange_RejectsWithoutHistory()
    {
        var loan = CreateDraft();
        loan.AddBorrower(Borrower.Create("Synthetic Borrower", "borrower@example.test", Money.Usd(125_000)), Now);
        loan.AddProperty(
            Property.Create("123 Demo Street", "Birmingham", "MI", "48009", Money.Usd(350_000)),
            Now.AddMinutes(2));
        loan.UpdateLoanTerms(Money.Usd(300_000), LoanPurpose.Purchase, 6.5m, 360, Now.AddMinutes(3));

        Assert.Throws<DomainValidationException>(
            () => loan.TransitionTo(LoanStatus.Submitted, ActorId, null, Now.AddMinutes(2)));

        Assert.Equal(LoanStatus.Draft, loan.Status);
        Assert.Null(loan.SubmittedUtc);
        Assert.Empty(loan.StatusHistory);
        Assert.Equal(Now.AddMinutes(3), loan.UpdatedUtc);
    }

    [Fact]
    public void TransitionToApproved_FromDraft_RejectsIt()
    {
        var loan = CreateCompleteDraft();

        Assert.Throws<DomainValidationException>(
            () => loan.TransitionTo(LoanStatus.Approved, ActorId, "Cannot jump states.", Now.AddMinutes(1)));

        Assert.Equal(LoanStatus.Draft, loan.Status);
    }

    [Fact]
    public void TransitionToUnderwriting_FromProcessing_RequiresReason()
    {
        var loan = CreateSubmittedLoan();
        loan.TransitionTo(LoanStatus.Processing, ActorId, null, Now.AddMinutes(2));

        Assert.Throws<DomainValidationException>(
            () => loan.TransitionTo(LoanStatus.Underwriting, ActorId, "", Now.AddMinutes(3)));

        Assert.Equal(LoanStatus.Processing, loan.Status);
    }

    [Theory]
    [InlineData(LoanStatus.Approved)]
    [InlineData(LoanStatus.Rejected)]
    public void TransitionFromUnderwriting_ToTerminalDecision_Succeeds(LoanStatus decision)
    {
        var loan = CreateUnderwritingLoan();

        loan.TransitionTo(decision, ActorId, "Reviewed synthetic loan package.", Now.AddMinutes(4));

        Assert.Equal(decision, loan.Status);
        Assert.Equal(4, loan.StatusHistory.Count);
    }

    [Theory]
    [InlineData(LoanStatus.Approved)]
    [InlineData(LoanStatus.Rejected)]
    public void TransitionFromTerminalState_RejectsFurtherChanges(LoanStatus terminalStatus)
    {
        var loan = CreateUnderwritingLoan();
        loan.TransitionTo(terminalStatus, ActorId, "Final decision.", Now.AddMinutes(4));

        Assert.Throws<DomainValidationException>(
            () => loan.TransitionTo(LoanStatus.MoreInformationRequired, ActorId, "Reopen.", Now.AddMinutes(5)));

        Assert.Equal(terminalStatus, loan.Status);
    }

    private static LoanApplication CreateUnderwritingLoan()
    {
        var loan = CreateSubmittedLoan();
        loan.TransitionTo(LoanStatus.Processing, ActorId, null, Now.AddMinutes(2));
        loan.TransitionTo(LoanStatus.Underwriting, ActorId, "Checklist complete.", Now.AddMinutes(3));
        return loan;
    }

    private static LoanApplication CreateSubmittedLoan()
    {
        var loan = CreateCompleteDraft();
        loan.TransitionTo(LoanStatus.Submitted, ActorId, null, Now.AddMinutes(1));
        return loan;
    }

    private static LoanApplication CreateCompleteDraft()
    {
        var loan = CreateDraft();
        loan.AddBorrower(Borrower.Create("Synthetic Borrower", "borrower@example.test", Money.Usd(125_000)), Now);
        loan.AddProperty(Property.Create("123 Demo Street", "Birmingham", "MI", "48009", Money.Usd(350_000)), Now);
        loan.UpdateLoanTerms(Money.Usd(300_000), LoanPurpose.Purchase, 6.5m, 360, Now.AddMinutes(1));
        return loan;
    }

    private static LoanApplication CreateDraft()
    {
        return LoanApplication.CreateDraft(LoanNumber.Create("MF-000001"), BrokerId, Money.Usd(300_000), Now);
    }
}
