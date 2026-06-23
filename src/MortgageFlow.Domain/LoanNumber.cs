using System.Text.RegularExpressions;

namespace MortgageFlow.Domain;

public sealed partial record LoanNumber
{
    private LoanNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static LoanNumber Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException("Loan number is required.");
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (!LoanNumberPattern().IsMatch(normalized))
        {
            throw new DomainValidationException("Loan number must use the format MF- followed by six digits.");
        }

        return new LoanNumber(normalized);
    }

    public override string ToString() => Value;

    [GeneratedRegex("^MF-[0-9]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex LoanNumberPattern();
}
