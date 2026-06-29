export function LoadingState({ label = 'Loading…' }: { label?: string }) {
  return <div className="state-panel">{label}</div>
}

export function EmptyState({ label }: { label: string }) {
  return <div className="state-panel muted">{label}</div>
}

export function ErrorState({ message }: { message: string }) {
  return (
    <div className="state-panel error" role="alert">
      {message}
    </div>
  )
}

export function FieldError({ message }: { message?: string }) {
  if (!message) {
    return null
  }

  return <p className="field-error">{message}</p>
}
