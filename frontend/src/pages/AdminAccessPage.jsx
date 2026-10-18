import { useEffect, useState } from 'react'
import { api } from '../services/api'
import ErrorMessage from '../components/ErrorMessage'
import Button from '../components/Button'

export default function AdminAccessPage() {
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')
  const [attempt, setAttempt] = useState(0)
  useEffect(() => {
    const controller = new AbortController()
    api('/admin/access', { signal: controller.signal }).then((data) => setMessage(data.message))
      .catch((failure) => { if (failure.name !== 'AbortError') setError(failure.status === 401 ? 'Your session has expired. Sign out and sign in again.' : failure.message) })
    return () => controller.abort()
  }, [attempt])
  return <>
    <h1 className="text-3xl font-bold">Administration</h1>
    <section className="space-y-4 rounded-2xl border border-slate-200 bg-white p-6">
      <ErrorMessage>{error}</ErrorMessage>
      {error ? <Button onClick={() => { setError(''); setMessage(''); setAttempt((value) => value + 1) }}>Retry</Button>
        : <p role="status" className="font-medium text-blue-700">{message || 'Verifying administrator access…'}</p>}
      <p className="text-sm leading-6 text-slate-600">Appointment management and clinic scheduling will be added in Phase 4.</p>
    </section>
  </>
}
