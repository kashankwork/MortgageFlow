import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { jsonResponse, mockFetch } from '../../test/testUtils'
import { TeamAssignmentPanel } from './TeamAssignmentPanel'

const loan = {
  id: 'loan-id',
  loanNumber: 'MF-TEST',
  brokerId: 'broker-id',
  assigneeId: null,
  status: 1 as const,
  businessPriority: 0 as const,
  requestedAmount: 300000,
  loanPurpose: 0 as const,
  interestRatePercent: 6.5,
  termMonths: 360,
  borrower: null,
  property: null,
  createdUtc: '2026-01-01T00:00:00Z',
  updatedUtc: '2026-01-01T00:00:00Z',
  submittedUtc: '2026-01-01T00:00:00Z',
  rowVersion: 'abc',
}

describe('TeamAssignmentPanel', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('requires a reason before reassignment', async () => {
    const user = userEvent.setup()
    renderPanel()

    expect(screen.getByRole('button', { name: /reassign/i })).toBeDisabled()
    await user.type(screen.getByLabelText(/new assignee id/i), 'employee-id')
    expect(screen.getByRole('button', { name: /reassign/i })).toBeDisabled()
    await user.type(screen.getByLabelText(/reason/i), 'Balance workload.')
    expect(screen.getByRole('button', { name: /reassign/i })).toBeEnabled()
  })

  it('shows concurrency guidance for stale priority updates', async () => {
    const user = userEvent.setup()
    mockFetch(() => jsonResponse({ title: 'Concurrency conflict.' }, 409))
    renderPanel()

    await user.type(screen.getByLabelText(/reason/i), 'Escalate.')
    await user.click(screen.getByRole('button', { name: /update priority/i }))

    expect(await screen.findByText(/refresh and try again/i)).toBeInTheDocument()
  })
})

function renderPanel() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  render(
    <QueryClientProvider client={queryClient}>
      <TeamAssignmentPanel loan={loan} />
    </QueryClientProvider>,
  )
}
