import type { ProblemDetails } from './types'

export class ApiError extends Error {
  public readonly status: number

  public readonly problem: ProblemDetails | null

  constructor(
    status: number,
    problem: ProblemDetails | null,
  ) {
    super(problem?.detail ?? problem?.title ?? `Request failed with status ${status}`)
    this.status = status
    this.problem = problem
  }

  get isValidationError() {
    return this.status === 400 && Boolean(this.problem?.errors)
  }
}

export type TokenProvider = () => string | null

let tokenProvider: TokenProvider = () => null
let unauthorizedHandler: (() => void) | null = null

export function configureApiClient(getToken: TokenProvider, onUnauthorized: () => void) {
  tokenProvider = getToken
  unauthorizedHandler = onUnauthorized
}

export async function apiRequest<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  const token = tokenProvider()

  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  if (init.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  const response = await fetch(path, { ...init, headers })

  if (response.status === 204) {
    return undefined as T
  }

  const text = await response.text()
  const payload = text ? tryParseJson(text) : null

  if (!response.ok) {
    if (response.status === 401) {
      unauthorizedHandler?.()
    }

    throw new ApiError(response.status, isProblemDetails(payload) ? payload : null)
  }

  return payload as T
}

export function jsonBody(value: unknown) {
  return JSON.stringify(value)
}

function tryParseJson(text: string): unknown {
  try {
    return JSON.parse(text)
  } catch {
    return { detail: text }
  }
}

function isProblemDetails(value: unknown): value is ProblemDetails {
  return typeof value === 'object' && value !== null
}

export function toUserMessage(error: unknown) {
  if (error instanceof ApiError) {
    if (error.status === 401) {
      return 'Please sign in again to continue.'
    }

    if (error.status === 403) {
      return 'Your role does not have permission for this action.'
    }

    if (error.status === 404) {
      return 'This record was not found or is hidden from your role.'
    }

    if (error.status === 409) {
      return 'This record changed in another session. Refresh and try again.'
    }

    return error.problem?.detail ?? error.problem?.title ?? error.message
  }

  return 'Something went wrong. Please try again.'
}
