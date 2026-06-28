using Microsoft.Extensions.Options;
using MortgageFlow.Application.Assignments;
using MortgageFlow.Domain;

namespace MortgageFlow.Infrastructure.Assignments;

public sealed class LoanPriorityCalculator : ILoanPriorityCalculator
{
    private readonly AssignmentOptions _options;

    public LoanPriorityCalculator(IOptions<AssignmentOptions> options)
    {
        _options = options.Value;
    }

    public int Calculate(LoanPriorityInput input)
    {
        var score = 0;

        if (input.EarliestDueUtc is { } dueUtc)
        {
            var timeUntilDue = dueUtc - input.NowUtc;
            score += timeUntilDue.TotalSeconds < 0
                ? _options.OverdueDueDateWeight
                : timeUntilDue.TotalDays <= 1
                    ? _options.DueWithinOneDayWeight
                    : timeUntilDue.TotalDays <= 3
                        ? _options.DueWithinThreeDaysWeight
                        : timeUntilDue.TotalDays <= 7
                            ? _options.DueWithinSevenDaysWeight
                            : 0;
        }

        score += input.BusinessPriority switch
        {
            BusinessPriority.High => _options.HighBusinessPriorityWeight,
            BusinessPriority.Urgent => _options.UrgentBusinessPriorityWeight,
            _ => 0
        };

        if (input.Status == LoanStatus.MoreInformationRequired)
        {
            score += _options.MoreInformationRequiredWeight;
        }

        var ageStart = input.SubmittedUtc ?? input.CreatedUtc;
        if (CountBusinessDays(ageStart.Date, input.NowUtc.Date) > _options.AgedLoanBusinessDays)
        {
            score += _options.AgedLoanWeight;
        }

        return Math.Clamp(score, 0, 100);
    }

    private static int CountBusinessDays(DateTime startDate, DateTime endDate)
    {
        if (endDate <= startDate)
        {
            return 0;
        }

        var days = 0;
        for (var date = startDate; date < endDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
            {
                days++;
            }
        }

        return days;
    }
}
