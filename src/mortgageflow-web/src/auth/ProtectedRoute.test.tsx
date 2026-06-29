import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { AppRoutes } from '../App'
import { renderWithProviders } from '../test/testUtils'

describe('ProtectedRoute', () => {
  it('redirects unauthenticated users to login', async () => {
    renderWithProviders(<AppRoutes />, { route: '/queues/team', role: null })

    expect(await screen.findByRole('heading', { name: /sign in with a synthetic role/i })).toBeInTheDocument()
  })
})
