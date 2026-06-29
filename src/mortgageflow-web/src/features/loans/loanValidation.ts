import { z } from 'zod'

export const loanFormSchema = z.object({
  requestedAmount: z.coerce.number().positive('Requested amount must be greater than zero.'),
  loanPurpose: z.coerce.number().min(0).max(1),
  interestRatePercent: z.coerce.number().min(0.1, 'Interest rate is required.').max(25),
  termMonths: z.coerce.number().int().min(60).max(480),
  borrowerFullName: z.string().min(2, 'Borrower name is required.'),
  borrowerEmail: z.string().email('Borrower email must be valid synthetic contact data.'),
  annualIncome: z.coerce.number().positive('Annual income is required.'),
  streetAddress: z.string().min(3, 'Street address is required.'),
  city: z.string().min(2, 'City is required.'),
  state: z.string().length(2, 'Use a two-letter state.'),
  postalCode: z.string().min(5, 'Postal code is required.'),
  estimatedValue: z.coerce.number().positive('Estimated value is required.'),
  occupancyType: z.coerce.number().min(0).max(2),
})

export type LoanFormValues = z.infer<typeof loanFormSchema>

export type LoanFormInput = z.input<typeof loanFormSchema>
