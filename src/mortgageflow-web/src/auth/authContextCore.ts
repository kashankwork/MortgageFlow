import { createContext } from 'react'
import type { AuthenticatedUser } from '../api/types'

export interface AuthContextValue {
  user: AuthenticatedUser | null
  isAuthenticated: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)
