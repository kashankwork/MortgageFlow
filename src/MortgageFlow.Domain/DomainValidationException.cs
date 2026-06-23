namespace MortgageFlow.Domain;

public sealed class DomainValidationException : InvalidOperationException
{
    public DomainValidationException(string message)
        : base(message)
    {
    }
}
