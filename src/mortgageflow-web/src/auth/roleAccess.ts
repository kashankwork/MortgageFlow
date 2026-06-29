import { loanStatuses, type LoanDetailResponse, type LoanStatus, type Role } from '../api/types'

export function canCreateLoan(role: Role) {
  return role === 'Broker'
}

export function canUsePersonalQueue(role: Role) {
  return role === 'Processor' || role === 'Underwriter'
}

export function canUseTeamQueue(role: Role) {
  return role === 'TeamLead'
}

export function canManageAssignment(role: Role) {
  return role === 'TeamLead'
}

export function canEditLoan(role: Role, loan: LoanDetailResponse) {
  return role === 'Broker' && loan.status === loanStatuses.draft
}

export function transitionOptions(role: Role, loan: LoanDetailResponse): Array<{ label: string; status: LoanStatus }> {
  if (role === 'Broker' && loan.status === loanStatuses.draft) {
    return [{ label: 'Submit loan', status: loanStatuses.submitted }]
  }

  if (role === 'Processor') {
    if (loan.status === loanStatuses.submitted) {
      return [{ label: 'Start processing', status: loanStatuses.processing }]
    }

    if (loan.status === loanStatuses.processing) {
      return [
        { label: 'Send to underwriting', status: loanStatuses.underwriting },
        { label: 'Request more information', status: loanStatuses.moreInformationRequired },
      ]
    }
  }

  if (role === 'Underwriter' && loan.status === loanStatuses.underwriting) {
    return [
      { label: 'Approve loan', status: loanStatuses.approved },
      { label: 'Reject loan', status: loanStatuses.rejected },
      { label: 'Request more information', status: loanStatuses.moreInformationRequired },
    ]
  }

  return []
}
