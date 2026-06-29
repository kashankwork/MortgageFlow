import { apiRequest, jsonBody } from './apiClient'
import type {
  AssignmentDecisionResponse,
  BusinessPriority,
  CreateLoanRequest,
  LoanDetailResponse,
  LoanListItemResponse,
  LoanStatus,
  LoanStatusHistoryResponse,
  PagedResult,
  PriorityUpdateResponse,
  UpdateLoanRequest,
} from './types'

export interface LoanListQuery {
  page?: number
  pageSize?: number
  search?: string
  status?: LoanStatus | ''
}

export function listLoans(query: LoanListQuery = {}) {
  return apiRequest<PagedResult<LoanListItemResponse>>(`/api/v1/loans${toQueryString(query)}`)
}

export function getLoan(id: string) {
  return apiRequest<LoanDetailResponse>(`/api/v1/loans/${id}`)
}

export function createLoan(request: CreateLoanRequest) {
  return apiRequest<LoanDetailResponse>('/api/v1/loans', {
    method: 'POST',
    body: jsonBody(request),
  })
}

export function updateLoan(id: string, request: UpdateLoanRequest) {
  return apiRequest<LoanDetailResponse>(`/api/v1/loans/${id}`, {
    method: 'PUT',
    body: jsonBody(request),
  })
}

export function transitionLoan(id: string, nextStatus: LoanStatus, reason: string | null, rowVersion: string) {
  return apiRequest<LoanDetailResponse>(`/api/v1/loans/${id}/transitions`, {
    method: 'POST',
    body: jsonBody({ nextStatus, reason, rowVersion }),
  })
}

export function getLoanHistory(id: string) {
  return apiRequest<LoanStatusHistoryResponse[]>(`/api/v1/loans/${id}/history`)
}

export function assignLoan(id: string) {
  return apiRequest<AssignmentDecisionResponse>(`/api/v1/loans/${id}/assign`, {
    method: 'POST',
  })
}

export function reassignLoan(id: string, newAssigneeId: string, reason: string, rowVersion: string) {
  return apiRequest<AssignmentDecisionResponse>(`/api/v1/loans/${id}/reassign`, {
    method: 'POST',
    body: jsonBody({ newAssigneeId, reason, rowVersion }),
  })
}

export function updatePriority(
  id: string,
  businessPriority: BusinessPriority,
  reason: string,
  rowVersion: string,
) {
  return apiRequest<PriorityUpdateResponse>(`/api/v1/loans/${id}/priority`, {
    method: 'PATCH',
    body: jsonBody({ businessPriority, reason, rowVersion }),
  })
}

function toQueryString(query: LoanListQuery) {
  const params = new URLSearchParams()

  if (query.page) params.set('page', String(query.page))
  if (query.pageSize) params.set('pageSize', String(query.pageSize))
  if (query.search) params.set('search', query.search)
  if (query.status !== undefined && query.status !== '') params.set('status', String(query.status))

  const value = params.toString()
  return value ? `?${value}` : ''
}
