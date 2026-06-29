import { apiRequest, jsonBody } from './apiClient'
import type { AuthenticatedUser } from './types'

export interface LoginRequest {
  email: string
  password: string
}

export function login(request: LoginRequest) {
  return apiRequest<AuthenticatedUser>('/api/v1/auth/login', {
    method: 'POST',
    body: jsonBody(request),
  })
}

export function getCurrentUser() {
  return apiRequest<AuthenticatedUser>('/api/v1/auth/me')
}
