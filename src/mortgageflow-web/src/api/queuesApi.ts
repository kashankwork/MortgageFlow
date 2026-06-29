import { apiRequest } from './apiClient'
import type { LoanStatus, PagedResult, QueueItemResponse } from './types'

export interface QueueQuery {
  page?: number
  pageSize?: number
  status?: LoanStatus | ''
}

export function getMyQueue(query: QueueQuery = {}) {
  return apiRequest<PagedResult<QueueItemResponse>>(`/api/v1/queues/me${toQueryString(query)}`)
}

export function getTeamQueue(query: QueueQuery = {}) {
  return apiRequest<PagedResult<QueueItemResponse>>(`/api/v1/queues/team${toQueryString(query)}`)
}

function toQueryString(query: QueueQuery) {
  const params = new URLSearchParams()

  if (query.page) params.set('page', String(query.page))
  if (query.pageSize) params.set('pageSize', String(query.pageSize))
  if (query.status !== undefined && query.status !== '') params.set('status', String(query.status))

  const value = params.toString()
  return value ? `?${value}` : ''
}
