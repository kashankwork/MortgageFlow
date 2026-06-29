import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { configureApiClient } from '../api/apiClient'
import { login as loginRequest } from '../api/authApi'
import type { AuthenticatedUser } from '../api/types'
import { AuthContext, type AuthContextValue } from './authContextCore'

interface AuthProviderProps {
  children: ReactNode
  initialUser?: AuthenticatedUser | null
}

export function AuthProvider({ children, initialUser = null }: AuthProviderProps) {
  const [user, setUser] = useState<AuthenticatedUser | null>(initialUser)

  useEffect(() => {
    configureApiClient(
      () => user?.accessToken ?? null,
      () => setUser(null),
    )
  }, [user])

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: Boolean(user),
      login: async (email, password) => {
        const authenticated = await loginRequest({ email, password })
        setUser(authenticated)
      },
      logout: () => setUser(null),
    }),
    [user],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
