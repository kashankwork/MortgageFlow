import { screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AppRoutes } from '../../App'
import { mockFetch, renderWithProviders } from '../../test/testUtils'

describe('QueuePage', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('renders an empty queue state', async () => {
    mockFetch(() => ({ items: [], page: 1, pageSize: 10, totalCount: 0 }))
    renderWithProviders(<AppRoutes />, { route: '/queues/me', role: 'Processor' })

    expect(await screen.findByText(/no active queue items/i)).toBeInTheDocument()
  })
})
