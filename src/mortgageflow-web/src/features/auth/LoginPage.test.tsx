import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AppRoutes } from '../../App'
import { jsonResponse, mockFetch, renderWithProviders } from '../../test/testUtils'

describe('LoginPage', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('shows required-field validation', async () => {
    const user = userEvent.setup()
    renderWithProviders(<AppRoutes />, { route: '/login', role: null })

    await user.clear(screen.getByLabelText(/email/i))
    await user.click(screen.getByRole('button', { name: /sign in/i }))

    expect(await screen.findByText(/valid synthetic account email/i)).toBeInTheDocument()
    expect(screen.getByText('Enter your local demo password.')).toBeInTheDocument()
  })

  it('shows a safe authentication failure', async () => {
    const user = userEvent.setup()
    mockFetch(() => jsonResponse({ title: 'Authentication failed.' }, 401))
    renderWithProviders(<AppRoutes />, { route: '/login', role: null })

    await user.type(screen.getByLabelText(/password/i), 'wrong-password')
    await user.click(screen.getByRole('button', { name: /sign in/i }))

    expect(await screen.findByText(/please sign in again/i)).toBeInTheDocument()
  })
})
