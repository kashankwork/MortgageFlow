import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { getMyQueue, getTeamQueue } from '../../api/queuesApi'
import { formatDate } from '../../api/formatters'
import { loanStatuses, priorityLabels, statusLabels, type LoanStatus, type QueueItemResponse } from '../../api/types'
import { PriorityBadge, StatusBadge } from '../../components/Badges'
import { EmptyState, ErrorState, LoadingState } from '../../components/States'

interface QueuePageProps {
  scope: 'me' | 'team'
}

export function QueuePage({ scope }: QueuePageProps) {
  const [status, setStatus] = useState<LoanStatus | ''>('')
  const [page, setPage] = useState(1)
  const query = useQuery({
    queryKey: ['queues', scope, { status, page }],
    queryFn: () => scope === 'team'
      ? getTeamQueue({ page, pageSize: 10, status })
      : getMyQueue({ page, pageSize: 10, status }),
  })

  return (
    <section className="panel">
      <div className="section-heading">
        <div>
          <p className="eyebrow">{scope === 'team' ? 'Team Lead view' : 'Assigned work'}</p>
          <h2>{scope === 'team' ? 'Team queue' : 'My queue'}</h2>
        </div>
      </div>

      <div className="help-panel">
        Queues are ordered by priority score, stage timing, and stable loan identifiers so urgent work stays visible.
      </div>

      <div className="filters">
        <label>
          Status
          <select value={status} onChange={(event) => { setStatus(parseStatus(event.target.value)); setPage(1) }}>
            <option value="">All active</option>
            {[loanStatuses.submitted, loanStatuses.processing, loanStatuses.underwriting, loanStatuses.moreInformationRequired].map((value) => (
              <option key={value} value={value}>{statusLabels[value]}</option>
            ))}
          </select>
        </label>
      </div>

      {query.isLoading ? <LoadingState label="Loading queue…" /> : null}
      {query.error ? <ErrorState message="Unable to load this queue for your role." /> : null}
      {query.data && query.data.items.length === 0 ? <EmptyState label="No active queue items match this view." /> : null}
      {query.data && query.data.items.length > 0 ? (
        <>
          <div className="queue-grid">
            {query.data.items.map((item) => <QueueCard key={item.loanId} item={item} />)}
          </div>
          <div className="pagination">
            <button type="button" className="secondary" disabled={page === 1} onClick={() => setPage((value) => value - 1)}>
              Previous
            </button>
            <span>Page {query.data.page} · {query.data.totalCount} total</span>
            <button
              type="button"
              className="secondary"
              disabled={page * query.data.pageSize >= query.data.totalCount}
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

function QueueCard({ item }: { item: QueueItemResponse }) {
  return (
    <article className="queue-card">
      <div className="section-heading compact">
        <div>
          <p className="eyebrow">{item.loanNumber}</p>
          <h3>{statusLabels[item.status]}</h3>
        </div>
        <PriorityBadge priority={item.businessPriority} score={item.priorityScore} />
      </div>
      <p><strong>Due:</strong> {formatDate(item.earliestDueUtc)}</p>
      <p><strong>Assignee:</strong> {item.assigneeName ?? 'Unassigned'}</p>
      {item.assigneeId ? <p className="muted"><strong>Assignee id:</strong> {item.assigneeId}</p> : null}
      <p><strong>Business priority:</strong> {priorityLabels[item.businessPriority]}</p>
      <div className="button-row">
        <StatusBadge status={item.status} />
        <Link to={`/loans/${item.loanId}`}>Open loan</Link>
      </div>
    </article>
  )
}

function parseStatus(value: string): LoanStatus | '' {
  return value === '' ? '' : Number(value) as LoanStatus
}
