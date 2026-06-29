import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import type React from 'react'
import { MemoryRouter } from 'react-router-dom'
import { vi } from 'vitest'
import { AuthProvider } from '../auth/AuthContext'
import type { AuthenticatedUser, Role } from '../api/types'

export function renderWithProviders(
  ui: React.ReactElement,
  options: { route?: string; role?: Role | null } = {},
) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  return render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider initialUser={options.role === null ? null : makeUser(options.role ?? 'TeamLead')}>
        <MemoryRouter initialEntries={[options.route ?? '/']}>{ui}</MemoryRouter>
      </AuthProvider>
    </QueryClientProvider>,
  )
}

export function makeUser(role: Role): AuthenticatedUser {
  return {
    userId: `${role.toLowerCase()}-user-id`,
    email: `${role.toLowerCase()}@example.test`,
    fullName: `Synthetic ${role}`,
    role,
    accessToken: 'test-token',
    expiresUtc: '2030-01-01T00:00:00Z',
  }
}

export function mockFetch(handler: (input: RequestInfo | URL, init?: RequestInit) => unknown) {
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const result = handler(input, init)
    if (result instanceof Response) {
      return result
    }

    return jsonResponse(result)
  })

  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

export function jsonResponse(value: unknown, status = 200) {
  return new Response(JSON.stringify(value), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}
