namespace MortgageFlow.Infrastructure.Persistence.Entities;

public sealed class EmployeeSkill
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string SkillTag { get; set; } = string.Empty;
}
