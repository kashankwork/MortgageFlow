export type Role = 'Broker' | 'Processor' | 'Underwriter' | 'TeamLead'

export type LoanStatus = 0 | 1 | 2 | 3 | 4 | 5 | 6
export type BusinessPriority = 0 | 1 | 2
export type LoanPurpose = 0 | 1
export type OccupancyType = 0 | 1 | 2

export const loanStatuses = {
  draft: 0,
  submitted: 1,
  processing: 2,
  underwriting: 3,
  moreInformationRequired: 4,
  approved: 5,
  rejected: 6,
} as const

export const businessPriorities = {
  normal: 0,
  high: 1,
  urgent: 2,
} as const

export const loanPurposes = {
  purchase: 0,
  refinance: 1,
} as const

export const occupancyTypes = {
  primaryResidence: 0,
  secondHome: 1,
  investmentProperty: 2,
} as const

export const statusLabels: Record<LoanStatus, string> = {
  0: 'Draft',
  1: 'Submitted',
  2: 'Processing',
  3: 'Underwriting',
  4: 'More Information Required',
  5: 'Approved',
  6: 'Rejected',
}

export const priorityLabels: Record<BusinessPriority, string> = {
  0: 'Normal',
  1: 'High',
  2: 'Urgent',
}

export const loanPurposeLabels: Record<LoanPurpose, string> = {
  0: 'Purchase',
  1: 'Refinance',
}

export const occupancyLabels: Record<OccupancyType, string> = {
  0: 'Primary Residence',
  1: 'Second Home',
  2: 'Investment Property',
}

export interface AuthenticatedUser {
  userId: string
  email: string
  fullName: string
  role: Role
  accessToken: string
  expiresUtc: string
}

export interface BorrowerDto {
  fullName: string
  email: string
  annualIncome: number
}

export interface PropertyDto {
  streetAddress: string
  city: string
  state: string
  postalCode: string
  estimatedValue: number
  occupancyType: OccupancyType
}

export interface CreateLoanRequest {
  requestedAmount: number
  loanPurpose: LoanPurpose | null
  interestRatePercent: number | null
  termMonths: number | null
  borrower: BorrowerDto | null
  property: PropertyDto | null
}

export interface UpdateLoanRequest extends CreateLoanRequest {
  rowVersion: string
}

export interface LoanDetailResponse {
  id: string
  loanNumber: string
  brokerId: string
  assigneeId: string | null
  status: LoanStatus
  businessPriority: BusinessPriority
  requestedAmount: number
  loanPurpose: LoanPurpose | null
  interestRatePercent: number | null
  termMonths: number | null
  borrower: BorrowerDto | null
  property: PropertyDto | null
  createdUtc: string
  updatedUtc: string
  submittedUtc: string | null
  rowVersion: string
}

export interface LoanListItemResponse {
  id: string
  loanNumber: string
  status: LoanStatus
  requestedAmount: number
  borrowerName: string | null
  createdUtc: string
  updatedUtc: string
  submittedUtc: string | null
}

export interface LoanStatusHistoryResponse {
  previousStatus: LoanStatus
  newStatus: LoanStatus
  actorId: string
  reason: string | null
  changedUtc: string
}

export interface QueueItemResponse {
  loanId: string
  loanNumber: string
  status: LoanStatus
  businessPriority: BusinessPriority
  priorityScore: number
  earliestDueUtc: string | null
  assigneeId: string | null
  assigneeName: string | null
  createdUtc: string
  updatedUtc: string
  submittedUtc: string | null
}

export interface AssignmentDecisionResponse {
  loanId: string
  loanNumber: string
  assigned: boolean
  assignmentId: string | null
  assigneeId: string | null
  assigneeName: string | null
  result: string
  priorityScore: number
  normalizedLoad: number | null
  rowVersion: string
}

export interface PriorityUpdateResponse {
  loanId: string
  loanNumber: string
  businessPriority: BusinessPriority
  priorityScore: number
  rowVersion: string
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  errors?: Record<string, string[]>
}
