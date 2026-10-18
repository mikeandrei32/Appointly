import { useState } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import { api } from '../services/api'
import AuthLayout from '../layouts/AuthLayout'
import Button from '../components/Button'
import Input from '../components/Input'
import ErrorMessage from '../components/ErrorMessage'

export default function RegisterPage() {
  const { user } = useAuth()
  const navigate = useNavigate()
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  if (user) return <Navigate to="/account" replace />
  async function submit(event) {
    event.preventDefault()
    const values = Object.fromEntries(new FormData(event.currentTarget))
    if (values.password !== values.confirmPassword) { setError('Passwords must match.'); return }
    setBusy(true)
    setError('')
    try {
      await api('/auth/register', { method: 'POST', body: values })
      navigate('/login', { replace: true, state: { registered: true } })
    } catch (failure) { setError(failure.message) }
    finally { setBusy(false) }
  }
  return <AuthLayout title="Create your account" description="Register as a patient to get started.">
    <form onSubmit={submit} className="space-y-5">
      <ErrorMessage>{error}</ErrorMessage>
      <Input label="Full name" name="fullName" autoComplete="name" required minLength={2} maxLength={100} />
      <Input label="Email" name="email" type="email" autoComplete="email" required maxLength={254} />
      <Input label="Password" name="password" type="password" autoComplete="new-password" required minLength={12} maxLength={128} hint="Use at least 12 characters, including uppercase, lowercase, a number, and a symbol." />
      <Input label="Confirm password" name="confirmPassword" type="password" autoComplete="new-password" required minLength={12} maxLength={128} />
      <Button type="submit" disabled={busy} className="w-full">{busy ? 'Creating account…' : 'Create account'}</Button>
    </form>
    <p className="mt-6 text-sm text-slate-600">Already registered? <Link to="/login" className="font-semibold text-blue-700 hover:underline">Sign in</Link></p>
  </AuthLayout>
}

