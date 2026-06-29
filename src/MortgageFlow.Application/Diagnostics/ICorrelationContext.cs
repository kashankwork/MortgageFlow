namespace MortgageFlow.Application.Diagnostics;

/// <summary>
/// Exposes the current request correlation identifier without coupling application or infrastructure code to ASP.NET Core.
/// </summary>
public interface ICorrelationContext
{
    string CorrelationId { get; }
}
