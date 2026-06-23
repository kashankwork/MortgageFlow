namespace MortgageFlow.Domain;

public sealed class Borrower
{
    private Borrower(string fullName, string email, Money annualIncome)
    {
        FullName = fullName;
        Email = email;
        AnnualIncome = annualIncome;
    }

    public string FullName { get; private set; }

    public string Email { get; private set; }

    public Money AnnualIncome { get; private set; }

    public static Borrower Create(string fullName, string email, Money annualIncome)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new DomainValidationException("Borrower name is required.");
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal))
        {
            throw new DomainValidationException("Borrower email must be valid synthetic contact data.");
        }

        return new Borrower(fullName.Trim(), email.Trim().ToLowerInvariant(), annualIncome);
    }
}
