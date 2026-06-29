namespace MortgageFlow.Application.Authentication;

/// <summary>
/// Central role names used by authorization policies, seeded users, and API tests.
/// </summary>
public static class MortgageFlowRoles
{
    public const string Broker = "Broker";
    public const string Processor = "Processor";
    public const string Underwriter = "Underwriter";
    public const string TeamLead = "TeamLead";

    public static readonly IReadOnlyCollection<string> All =
    [
        Broker,
        Processor,
        Underwriter,
        TeamLead
    ];
}
