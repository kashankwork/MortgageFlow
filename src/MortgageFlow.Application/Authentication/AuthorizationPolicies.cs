namespace MortgageFlow.Application.Authentication;

public static class AuthorizationPolicies
{
    public const string Broker = "BrokerOnly";
    public const string Processor = "ProcessorOnly";
    public const string Underwriter = "UnderwriterOnly";
    public const string TeamLead = "TeamLeadOnly";
}
