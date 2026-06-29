import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useLocation, useNavigate } from 'react-router-dom'
import { z } from 'zod'
import { toUserMessage } from '../../api/apiClient'
import { useAuth } from '../../auth/useAuth'
import { FieldError } from '../../components/States'

const loginSchema = z.object({
  email: z.string().email('Enter a valid synthetic account email.'),
  password: z.string().min(1, 'Enter your local demo password.'),
})

type LoginForm = z.infer<typeof loginSchema>

const demoAccounts = [
  { label: 'Broker', email: 'broker@example.test' },
  { label: 'Processor', email: 'processor@example.test' },
  { label: 'Underwriter', email: 'underwriter@example.test' },
  { label: 'Team Lead', email: 'teamlead@example.test' },
] as const

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [error, setError] = useState<string | null>(null)
  const from = typeof location.state === 'object' && location.state && 'from' in location.state
    ? String(location.state.from)
    : '/loans'

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<LoginForm>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: 'teamlead@example.test', password: '' },
  })

  async function onSubmit(values: LoginForm) {
    setError(null)

    try {
      await login(values.email, values.password)
      navigate(from, { replace: true })
    } catch (caught) {
      setError(toUserMessage(caught))
    }
  }

  return (
    <section className="login-card">
      <div>
        <p className="eyebrow">Secure demo access</p>
        <h2>Sign in with a synthetic role</h2>
        <p className="muted">
          Role shortcuts fill the email only. Use your private local demo password from runtime configuration.
        </p>
      </div>

      <div className="shortcut-grid" aria-label="Synthetic account shortcuts">
        {demoAccounts.map((account) => (
          <button
            key={account.email}
            type="button"
            className="secondary"
            onClick={() => setValue('email', account.email, { shouldValidate: true })}
          >
            {account.label}
          </button>
        ))}
      </div>

      <form className="stack" onSubmit={handleSubmit(onSubmit)}>
        <label>
          Email
          <input {...register('email')} autoComplete="username" />
          <FieldError message={errors.email?.message} />
        </label>

        <label>
          Password
          <input {...register('password')} type="password" autoComplete="current-password" />
          <FieldError message={errors.password?.message} />
        </label>

        {error ? <div className="error" role="alert">{error}</div> : null}

        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Signing in…' : 'Sign in'}
        </button>
      </form>
    </section>
  )
}
