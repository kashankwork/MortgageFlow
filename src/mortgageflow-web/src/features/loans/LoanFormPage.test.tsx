import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { AppRoutes } from '../../App'
import { renderWithProviders } from '../../test/testUtils'

describe('LoanFormPage', () => {
  it('validates required borrower and property inputs', async () => {
    const user = userEvent.setup()
    renderWithProviders(<AppRoutes />, { route: '/loans/new', role: 'Broker' })

    await user.clear(screen.getByLabelText(/full name/i))
    await user.click(screen.getByRole('button', { name: /save and submit/i }))

    expect(await screen.findByText(/borrower name is required/i)).toBeInTheDocument()
    expect(screen.getByText(/street address is required/i)).toBeInTheDocument()
  })
})
