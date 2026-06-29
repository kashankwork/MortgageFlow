using MortgageFlow.Application.Authentication;
using MortgageFlow.Domain;

namespace MortgageFlow.Infrastructure.Assignments;

internal static class AssignmentConstants
{
    public static bool IsActiveQueueStatus(LoanStatus status)
    {
        return status is LoanStatus.Submitted
            or LoanStatus.Processing
            or LoanStatus.Underwriting
            or LoanStatus.MoreInformationRequired;
    }

    public static bool TryGetRouting(LoanStatus status, AssignmentOptions options, out string role, out string skill)
    {
        switch (status)
        {
            case LoanStatus.Submitted:
            case LoanStatus.Processing:
            case LoanStatus.MoreInformationRequired:
                role = MortgageFlowRoles.Processor;
                skill = options.ProcessingSkillTag;
                return true;
            case LoanStatus.Underwriting:
                role = MortgageFlowRoles.Underwriter;
                skill = options.UnderwritingSkillTag;
                return true;
            default:
                role = string.Empty;
                skill = string.Empty;
                return false;
        }
    }

    public static string CreateRoutingKey(Guid teamId, string role, string skill)
    {
        return $"{teamId:N}:{role}:{skill.ToLowerInvariant()}";
    }
}
