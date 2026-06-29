import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { listLoans } from '../../api/loansApi'
import { loanStatuses, statusLabels, type LoanStatus } from '../../api/types'
import { formatDate, formatMoney } from '../../api/formatters'
import { useAuth } from '../../auth/useAuth'
import { canCreateLoan } from '../../auth/roleAccess'
import { StatusBadge } from '../../components/Badges'
import { EmptyState, ErrorState, LoadingState } from '../../components/States'

export function LoanListPage() {
  const { user } = useAuth()
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<LoanStatus | ''>('')
  const [page, setPage] = useState(1)

  const loansQuery = useQuery({
    queryKey: ['loans', { page, search, status }],
    queryFn: () => listLoans({ page, pageSize: 10, search, status }),
  })

  return (
    <section className="panel">
      <div className="section-heading">
        <div>
          <p className="eyebrow">Loan workspace</p>
          <h2>Loans</h2>
        </div>
        {user && canCreateLoan(user.role) ? (
          <Link to="/loans/new" className="link-button">Create loan</Link>
        ) : null}
      </div>

      <div className="filters">
        <label>
          Search
          <input value={search} onChange={(event) => { setSearch(event.target.value); setPage(1) }} />
        </label>
        <label>
          Status
          <select value={status} onChange={(event) => { setStatus(parseStatus(event.target.value)); setPage(1) }}>
            <option value="">All</option>
            {Object.values(loanStatuses).map((value) => (
              <option key={value} value={value}>{statusLabels[value]}</option>
            ))}
          </select>
        </label>
      </div>

      {loansQuery.isLoading ? <LoadingState label="Loading loans…" /> : null}
      {loansQuery.error ? <ErrorState message="Unable to load loans for this role." /> : null}
      {loansQuery.data && loansQuery.data.items.length === 0 ? <EmptyState label="No loans match this view." /> : null}

      {loansQuery.data && loansQuery.data.items.length > 0 ? (
        <>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Loan</th>
                  <th>Borrower</th>
                  <th>Status</th>
                  <th>Amount</th>
                  <th>Updated</th>
                  <th>Action</th>
                </tr>
              </thead>
              <tbody>
                {loansQuery.data.items.map((loan) => (
                  <tr key={loan.id}>
                    <td>{loan.loanNumber}</td>
                    <td>{loan.borrowerName ?? 'Draft borrower'}</td>
                    <td><StatusBadge status={loan.status} /></td>
                    <td>{formatMoney(loan.requestedAmount)}</td>
                    <td>{formatDate(loan.updatedUtc)}</td>
                    <td><Link to={`/loans/${loan.id}`}>Review</Link></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="pagination">
            <button type="button" className="secondary" disabled={page === 1} onClick={() => setPage((value) => value - 1)}>
              Previous
            </button>
            <span>Page {loansQuery.data.page} · {loansQuery.data.totalCount} total</span>
            <button
              type="button"
              className="secondary"
              disabled={page * loansQuery.data.pageSize >= loansQuery.data.totalCount}
              onClick={() => setPage((value) => value + 1)}
            >
              Next
            </button>
          </div>
        </>
      ) : null}
    </section>
  )
}

function parseStatus(value: string): LoanStatus | '' {
  return value === '' ? '' : Number(value) as LoanStatus
}
