using MortgageFlow.Domain;

namespace MortgageFlow.Application.Loans;

/// <summary>
/// Orchestrates loan workflow use cases while keeping HTTP and EF details outside the API contract.
/// </summary>
public interface ILoanWorkflowService
{
    Task<LoanActionResult<LoanDetailResponse>> CreateDraftAsync(
        CreateLoanRequest request,
        CancellationToken cancellationToken);

    Task<LoanActionResult<PagedResult<LoanListItemResponse>>> ListAsync(
        int page,
        int pageSize,
        string? search,
        LoanStatus? status,
        CancellationToken cancellationToken);

    Task<LoanActionResult<LoanDetailResponse>> GetDetailAsync(Guid id, CancellationToken cancellationToken);

    Task<LoanActionResult<LoanDetailResponse>> UpdateDraftAsync(
        Guid id,
        UpdateLoanRequest request,
        CancellationToken cancellationToken);

    Task<LoanActionResult<LoanDetailResponse>> TransitionAsync(
        Guid id,
        TransitionLoanRequest request,
        CancellationToken cancellationToken);

    Task<LoanActionResult<IReadOnlyCollection<LoanStatusHistoryResponse>>> GetHistoryAsync(
        Guid id,
        CancellationToken cancellationToken);
}
