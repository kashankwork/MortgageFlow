import { priorityLabels, statusLabels, type BusinessPriority, type LoanStatus } from '../api/types'

export function StatusBadge({ status }: { status: LoanStatus }) {
  return <span className={`badge status-${status}`}>{statusLabels[status]}</span>
}

export function PriorityBadge({ priority, score }: { priority: BusinessPriority; score?: number }) {
  return (
    <span className={`badge priority-${priority}`}>
      {priorityLabels[priority]}
      {score !== undefined ? ` · ${score}` : ''}
    </span>
  )
}
