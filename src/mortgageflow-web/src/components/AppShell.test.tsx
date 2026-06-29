import { screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AppRoutes } from '../App'
import { mockFetch, renderWithProviders } from '../test/testUtils'

describe('AppShell navigation', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('shows Team Lead navigation without broker-only links', async () => {
    mockFetch(() => ({ items: [], page: 1, pageSize: 10, totalCount: 0 }))
    renderWithProviders(<AppRoutes />, { route: '/loans', role: 'TeamLead' })

    expect(await screen.findByRole('link', { name: /team queue/i })).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: /new loan/i })).not.toBeInTheDocument()
  })
})
