import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getLoan, getLoanHistory, transitionLoan } from '../../api/loansApi'
import { loanPurposeLabels, occupancyLabels, statusLabels, type LoanStatus } from '../../api/types'
import { toUserMessage } from '../../api/apiClient'
import { formatDate, formatMoney } from '../../api/formatters'
import { useAuth } from '../../auth/useAuth'
import { canEditLoan, canManageAssignment, transitionOptions } from '../../auth/roleAccess'
import { PriorityBadge, StatusBadge } from '../../components/Badges'
import { EmptyState, ErrorState, LoadingState } from '../../components/States'
import { TeamAssignmentPanel } from '../assignment/TeamAssignmentPanel'

export function LoanDetailsPage() {
  const { id } = useParams()
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)

  const loanQuery = useQuery({
    queryKey: ['loan', id],
    queryFn: () => getLoan(id ?? ''),
    enabled: Boolean(id),
  })

  const historyQuery = useQuery({
    queryKey: ['loan-history', id],
    queryFn: () => getLoanHistory(id ?? ''),
    enabled: Boolean(id),
  })

  const transitionMutation = useMutation({
    mutationFn: ({ status }: { status: LoanStatus }) => {
      if (!loanQuery.data) {
        throw new Error('Loan is not loaded.')
      }

      return transitionLoan(loanQuery.data.id, status, reason.trim() || null, loanQuery.data.rowVersion)
    },
    onSuccess: async (updated) => {
      setError(null)
      setReason('')
      await queryClient.invalidateQueries({ queryKey: ['loan', updated.id] })
      await queryClient.invalidateQueries({ queryKey: ['loan-history', updated.id] })
      await queryClient.invalidateQueries({ queryKey: ['queues'] })
      await queryClient.invalidateQueries({ queryKey: ['loans'] })
    },
    onError: (caught) => setError(toUserMessage(caught)),
  })

  if (loanQuery.isLoading) {
    return <LoadingState label="Loading loan details…" />
  }

  if (loanQuery.error || !loanQuery.data) {
    return <ErrorState message="Unable to load this loan. It may be hidden from your role." />
  }

  const loan = loanQuery.data
  const actions = user ? transitionOptions(user.role, loan) : []

  return (
    <section className="panel">
      <div className="section-heading">
        <div>
          <p className="eyebrow">Loan detail</p>
          <h2>{loan.loanNumber}</h2>
        </div>
        <div className="button-row">
          {user && canEditLoan(user.role, loan) ? <Link to={`/loans/${loan.id}/edit`} className="link-button">Edit draft</Link> : null}
          <Link to="/loans" className="secondary link-button">Back</Link>
        </div>
      </div>

      <div className="summary-grid">
        <div className="metric-card">
          <span>Status</span>
          <StatusBadge status={loan.status} />
        </div>
        <div className="metric-card">
          <span>Priority</span>
          <PriorityBadge priority={loan.businessPriority} />
        </div>
        <div className="metric-card">
          <span>Requested amount</span>
          <strong>{formatMoney(loan.requestedAmount)}</strong>
        </div>
        <div className="metric-card">
          <span>Assignee</span>
          <strong>{loan.assigneeId ?? 'Unassigned'}</strong>
        </div>
      </div>

      <div className="details-grid">
        <section className="subpanel">
          <h3>Borrower and property</h3>
          <dl>
            <dt>Borrower</dt>
            <dd>{loan.borrower?.fullName ?? 'Not provided'}</dd>
            <dt>Email</dt>
            <dd>{loan.borrower?.email ?? 'Not provided'}</dd>
            <dt>Income</dt>
            <dd>{loan.borrower ? formatMoney(loan.borrower.annualIncome) : 'Not provided'}</dd>
            <dt>Property</dt>
            <dd>{loan.property ? `${loan.property.streetAddress}, ${loan.property.city}, ${loan.property.state}` : 'Not provided'}</dd>
            <dt>Occupancy</dt>
            <dd>{loan.property ? occupancyLabels[loan.property.occupancyType] : 'Not provided'}</dd>
          </dl>
        </section>

        <section className="subpanel">
          <h3>Loan terms</h3>
          <dl>
            <dt>Purpose</dt>
            <dd>{loan.loanPurpose === null ? 'Not provided' : loanPurposeLabels[loan.loanPurpose]}</dd>
            <dt>Rate</dt>
            <dd>{loan.interestRatePercent ? `${loan.interestRatePercent}%` : 'Not provided'}</dd>
            <dt>Term</dt>
            <dd>{loan.termMonths ? `${loan.termMonths} months` : 'Not provided'}</dd>
            <dt>Submitted</dt>
            <dd>{formatDate(loan.submittedUtc)}</dd>
          </dl>
        </section>
      </div>

      <section className="subpanel">
        <h3>Workflow actions</h3>
        {error ? <div className="error" role="alert">{error}</div> : null}
        {actions.length === 0 ? <EmptyState label="No workflow actions are available for this role and status." /> : null}
        {actions.length > 0 ? (
          <>
            <label>
              Reason or rationale
              <textarea value={reason} onChange={(event) => setReason(event.target.value)} placeholder="Required for some workflow decisions" />
            </label>
            <div className="button-row">
              {actions.map((action) => (
                <button
                  key={action.status}
                  type="button"
                  onClick={() => transitionMutation.mutate({ status: action.status })}
                  disabled={transitionMutation.isPending}
                >
                  {action.label}
                </button>
              ))}
            </div>
          </>
        ) : null}
      </section>

      {user && canManageAssignment(user.role) ? <TeamAssignmentPanel loan={loan} /> : null}

      <section className="subpanel">
        <h3>Status timeline</h3>
        {historyQuery.isLoading ? <LoadingState label="Loading history…" /> : null}
        {historyQuery.data && historyQuery.data.length === 0 ? <EmptyState label="No status changes have been recorded yet." /> : null}
        {historyQuery.data && historyQuery.data.length > 0 ? (
          <ol className="timeline">
            {historyQuery.data.map((item) => (
              <li key={`${item.changedUtc}-${item.newStatus}`}>
                <strong>{statusLabels[item.previousStatus]} → {statusLabels[item.newStatus]}</strong>
                <span>{formatDate(item.changedUtc)}</span>
                {item.reason ? <p>{item.reason}</p> : null}
              </li>
            ))}
          </ol>
        ) : null}
      </section>
    </section>
  )
}
