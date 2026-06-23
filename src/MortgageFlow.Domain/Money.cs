namespace MortgageFlow.Domain;

public sealed record Money
{
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Money Usd(decimal amount)
    {
        if (amount < 0)
        {
            throw new DomainValidationException("Money amount cannot be negative.");
        }

        return new Money(decimal.Round(amount, 2), "USD");
    }

    public override string ToString() => $"{Currency} {Amount:0.00}";
}
