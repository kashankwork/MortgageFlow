import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { assignLoan, reassignLoan, updatePriority } from '../../api/loansApi'
import {
  businessPriorities,
  priorityLabels,
  type BusinessPriority,
  type LoanDetailResponse,
} from '../../api/types'
import { toUserMessage } from '../../api/apiClient'

interface TeamAssignmentPanelProps {
  loan: LoanDetailResponse
}

export function TeamAssignmentPanel({ loan }: TeamAssignmentPanelProps) {
  const queryClient = useQueryClient()
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [newAssigneeId, setNewAssigneeId] = useState('')
  const [reason, setReason] = useState('')
  const [priority, setPriority] = useState<BusinessPriority>(loan.businessPriority)

  async function refresh() {
    await queryClient.invalidateQueries({ queryKey: ['loan', loan.id] })
    await queryClient.invalidateQueries({ queryKey: ['queues'] })
    await queryClient.invalidateQueries({ queryKey: ['loans'] })
  }

  const assignMutation = useMutation({
    mutationFn: () => assignLoan(loan.id),
    onSuccess: async (result) => {
      setError(null)
      setMessage(result.assigned
        ? `Assigned to ${result.assigneeName} (${result.assigneeId}) with priority score ${result.priorityScore}.`
        : result.result)
      await refresh()
    },
    onError: (caught) => {
      setMessage(null)
      setError(toUserMessage(caught))
    },
  })

  const reassignMutation = useMutation({
    mutationFn: () => reassignLoan(loan.id, newAssigneeId.trim(), reason.trim(), loan.rowVersion),
    onSuccess: async (result) => {
      setError(null)
      setMessage(`Reassigned to ${result.assigneeName} (${result.assigneeId}).`)
      setReason('')
      await refresh()
    },
    onError: (caught) => {
      setMessage(null)
      setError(toUserMessage(caught))
    },
  })

  const priorityMutation = useMutation({
    mutationFn: () => updatePriority(loan.id, priority, reason.trim(), loan.rowVersion),
    onSuccess: async (result) => {
      setError(null)
      setMessage(`Priority updated to ${priorityLabels[result.businessPriority]} with score ${result.priorityScore}.`)
      setReason('')
      await refresh()
    },
    onError: (caught) => {
      setMessage(null)
      setError(toUserMessage(caught))
    },
  })

  const reasonMissing = reason.trim().length === 0

  return (
    <section className="subpanel">
      <div>
        <p className="eyebrow">Team Lead controls</p>
        <h3>Assignment and priority</h3>
        <p className="muted">Server-side authorization and row-version checks still control these actions.</p>
      </div>

      {message ? <div className="success" role="status">{message}</div> : null}
      {error ? <div className="error" role="alert">{error}</div> : null}

      <div className="button-row">
        <button type="button" onClick={() => assignMutation.mutate()} disabled={assignMutation.isPending}>
          Auto-assign
        </button>
      </div>

      <label>
        New assignee id
        <input value={newAssigneeId} onChange={(event) => setNewAssigneeId(event.target.value)} placeholder="Paste eligible employee id" />
      </label>

      <label>
        Reason
        <textarea value={reason} onChange={(event) => setReason(event.target.value)} placeholder="Required for reassignment and priority changes" />
      </label>

      <div className="button-row">
        <button
          type="button"
          disabled={newAssigneeId.trim().length === 0 || reasonMissing || reassignMutation.isPending}
          onClick={() => reassignMutation.mutate()}
        >
          Reassign
        </button>

        <select value={priority} onChange={(event) => setPriority(Number(event.target.value) as BusinessPriority)}>
          <option value={businessPriorities.normal}>Normal</option>
          <option value={businessPriorities.high}>High</option>
          <option value={businessPriorities.urgent}>Urgent</option>
        </select>

        <button
          type="button"
          disabled={reasonMissing || priorityMutation.isPending}
          onClick={() => priorityMutation.mutate()}
        >
          Update priority
        </button>
      </div>
    </section>
  )
}
