namespace MortgageFlow.Domain;

public enum LoanStatus
{
    Draft = 0,
    Submitted = 1,
    Processing = 2,
    Underwriting = 3,
    MoreInformationRequired = 4,
    Approved = 5,
    Rejected = 6
}
