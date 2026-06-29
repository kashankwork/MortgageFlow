namespace MortgageFlow.Infrastructure.Assignments;

public sealed class AssignmentOptions
{
    public const string SectionName = "Assignment";

    public int OverdueDueDateWeight { get; init; } = 50;

    public int DueWithinOneDayWeight { get; init; } = 30;

    public int DueWithinThreeDaysWeight { get; init; } = 20;

    public int DueWithinSevenDaysWeight { get; init; } = 10;

    public int HighBusinessPriorityWeight { get; init; } = 15;

    public int UrgentBusinessPriorityWeight { get; init; } = 30;

    public int MoreInformationRequiredWeight { get; init; } = 10;

    public int AgedLoanWeight { get; init; } = 10;

    public int AgedLoanBusinessDays { get; init; } = 10;

    public int ActiveLoanWeight { get; init; } = 2;

    public string ProcessingSkillTag { get; init; } = "processing";

    public string UnderwritingSkillTag { get; init; } = "underwriting";
}
