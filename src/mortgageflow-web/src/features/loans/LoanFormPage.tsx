import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ApiError, toUserMessage } from '../../api/apiClient'
import { createLoan, getLoan, transitionLoan, updateLoan } from '../../api/loansApi'
import {
  loanPurposes,
  loanStatuses,
  occupancyTypes,
  type CreateLoanRequest,
  type LoanDetailResponse,
} from '../../api/types'
import { FieldError, LoadingState } from '../../components/States'
import { loanFormSchema, type LoanFormInput, type LoanFormValues } from './loanValidation'

const defaultValues: LoanFormValues = {
  requestedAmount: 325000,
  loanPurpose: loanPurposes.purchase,
  interestRatePercent: 6.75,
  termMonths: 360,
  borrowerFullName: '',
  borrowerEmail: '',
  annualIncome: 95000,
  streetAddress: '',
  city: '',
  state: 'MI',
  postalCode: '',
  estimatedValue: 410000,
  occupancyType: occupancyTypes.primaryResidence,
}

export function LoanFormPage() {
  const { id } = useParams()
  const isEdit = Boolean(id)
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const [submitAfterSave, setSubmitAfterSave] = useState(false)

  const loanQuery = useQuery({
    queryKey: ['loan', id],
    queryFn: () => getLoan(id ?? ''),
    enabled: isEdit,
  })

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<LoanFormInput, unknown, LoanFormValues>({
    resolver: zodResolver(loanFormSchema),
    defaultValues,
  })

  useEffect(() => {
    if (loanQuery.data) {
      reset(toFormValues(loanQuery.data))
    }
  }, [loanQuery.data, reset])

  const saveMutation = useMutation({
    mutationFn: async (values: LoanFormValues) => {
      const request = toRequest(values)
      const saved = isEdit && loanQuery.data
        ? await updateLoan(loanQuery.data.id, { ...request, rowVersion: loanQuery.data.rowVersion })
        : await createLoan(request)

      if (submitAfterSave) {
        return transitionLoan(saved.id, loanStatuses.submitted, null, saved.rowVersion)
      }

      return saved
    },
    onSuccess: async (saved) => {
      await queryClient.invalidateQueries({ queryKey: ['loans'] })
      await queryClient.invalidateQueries({ queryKey: ['loan', saved.id] })
      navigate(`/loans/${saved.id}`)
    },
    onError: (caught) => setError(toUserMessage(caught)),
  })

  async function onSubmit(values: LoanFormValues) {
    setError(null)
    await saveMutation.mutateAsync(values)
  }

  if (loanQuery.isLoading) {
    return <LoadingState label="Loading loan draft…" />
  }

  return (
    <section className="panel">
      <div className="section-heading">
        <div>
          <p className="eyebrow">{isEdit ? 'Draft update' : 'Broker intake'}</p>
          <h2>{isEdit ? 'Edit loan draft' : 'Create loan draft'}</h2>
        </div>
        <Link to="/loans" className="secondary link-button">Back to loans</Link>
      </div>

      {error ? <div className="error" role="alert">{error}</div> : null}
      {loanQuery.error instanceof ApiError && loanQuery.error.status === 409 ? (
        <div className="error" role="alert">This draft changed elsewhere. Refresh before saving again.</div>
      ) : null}

      <form className="loan-form" onSubmit={handleSubmit(onSubmit)}>
        <fieldset>
          <legend>Borrower</legend>
          <label>
            Full name
            <input {...register('borrowerFullName')} />
            <FieldError message={errors.borrowerFullName?.message} />
          </label>
          <label>
            Email
            <input {...register('borrowerEmail')} />
            <FieldError message={errors.borrowerEmail?.message} />
          </label>
          <label>
            Annual income
            <input {...register('annualIncome')} type="number" />
            <FieldError message={errors.annualIncome?.message} />
          </label>
        </fieldset>

        <fieldset>
          <legend>Property</legend>
          <label>
            Street address
            <input {...register('streetAddress')} />
            <FieldError message={errors.streetAddress?.message} />
          </label>
          <label>
            City
            <input {...register('city')} />
            <FieldError message={errors.city?.message} />
          </label>
          <label>
            State
            <input {...register('state')} maxLength={2} />
            <FieldError message={errors.state?.message} />
          </label>
          <label>
            Postal code
            <input {...register('postalCode')} />
            <FieldError message={errors.postalCode?.message} />
          </label>
          <label>
            Estimated value
            <input {...register('estimatedValue')} type="number" />
            <FieldError message={errors.estimatedValue?.message} />
          </label>
          <label>
            Occupancy
            <select {...register('occupancyType')}>
              <option value={occupancyTypes.primaryResidence}>Primary Residence</option>
              <option value={occupancyTypes.secondHome}>Second Home</option>
              <option value={occupancyTypes.investmentProperty}>Investment Property</option>
            </select>
          </label>
        </fieldset>

        <fieldset>
          <legend>Loan terms</legend>
          <label>
            Requested amount
            <input {...register('requestedAmount')} type="number" />
            <FieldError message={errors.requestedAmount?.message} />
          </label>
          <label>
            Purpose
            <select {...register('loanPurpose')}>
              <option value={loanPurposes.purchase}>Purchase</option>
              <option value={loanPurposes.refinance}>Refinance</option>
            </select>
          </label>
          <label>
            Interest rate
            <input {...register('interestRatePercent')} type="number" step="0.01" />
            <FieldError message={errors.interestRatePercent?.message} />
          </label>
          <label>
            Term months
            <input {...register('termMonths')} type="number" />
            <FieldError message={errors.termMonths?.message} />
          </label>
        </fieldset>

        <div className="button-row">
          <button type="submit" disabled={isSubmitting} onClick={() => setSubmitAfterSave(false)}>
            Save draft
          </button>
          <button type="submit" disabled={isSubmitting} onClick={() => setSubmitAfterSave(true)}>
            Save and submit
          </button>
        </div>
      </form>
    </section>
  )
}

function toRequest(values: LoanFormValues): CreateLoanRequest {
  return {
    requestedAmount: values.requestedAmount,
    loanPurpose: values.loanPurpose as CreateLoanRequest['loanPurpose'],
    interestRatePercent: values.interestRatePercent,
    termMonths: values.termMonths,
    borrower: {
      fullName: values.borrowerFullName,
      email: values.borrowerEmail,
      annualIncome: values.annualIncome,
    },
    property: {
      streetAddress: values.streetAddress,
      city: values.city,
      state: values.state.toUpperCase(),
      postalCode: values.postalCode,
      estimatedValue: values.estimatedValue,
      occupancyType: values.occupancyType as NonNullable<CreateLoanRequest['property']>['occupancyType'],
    },
  }
}

function toFormValues(loan: LoanDetailResponse): LoanFormValues {
  return {
    requestedAmount: loan.requestedAmount,
    loanPurpose: loan.loanPurpose ?? loanPurposes.purchase,
    interestRatePercent: loan.interestRatePercent ?? defaultValues.interestRatePercent,
    termMonths: loan.termMonths ?? defaultValues.termMonths,
    borrowerFullName: loan.borrower?.fullName ?? '',
    borrowerEmail: loan.borrower?.email ?? '',
    annualIncome: loan.borrower?.annualIncome ?? defaultValues.annualIncome,
    streetAddress: loan.property?.streetAddress ?? '',
    city: loan.property?.city ?? '',
    state: loan.property?.state ?? 'MI',
    postalCode: loan.property?.postalCode ?? '',
    estimatedValue: loan.property?.estimatedValue ?? defaultValues.estimatedValue,
    occupancyType: loan.property?.occupancyType ?? occupancyTypes.primaryResidence,
  }
}
