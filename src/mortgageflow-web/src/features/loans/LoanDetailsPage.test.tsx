import { screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AppRoutes } from '../../App'
import { loanStatuses } from '../../api/types'
import { mockFetch, renderWithProviders } from '../../test/testUtils'

describe('LoanDetailsPage', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('does not present unavailable workflow actions', async () => {
    mockFetch((input) => {
      const url = String(input)
      if (url.endsWith('/history')) {
        return []
      }

      return {
        id: 'loan-id',
        loanNumber: 'MF-TEST',
        brokerId: 'broker-id',
        assigneeId: null,
        status: loanStatuses.draft,
        businessPriority: 0,
        requestedAmount: 300000,
        loanPurpose: 0,
        interestRatePercent: 6.5,
        termMonths: 360,
        borrower: null,
        property: null,
        createdUtc: '2026-01-01T00:00:00Z',
        updatedUtc: '2026-01-01T00:00:00Z',
        submittedUtc: null,
        rowVersion: 'abc',
      }
    })

    renderWithProviders(<AppRoutes />, { route: '/loans/loan-id', role: 'Processor' })

    expect(await screen.findByText(/no workflow actions are available/i)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /start processing/i })).not.toBeInTheDocument()
  })
})
