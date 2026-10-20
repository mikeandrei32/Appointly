import { useState } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import AuthLayout from '../layouts/AuthLayout'
import Button from '../components/Button'
import Input from '../components/Input'
import ErrorMessage from '../components/ErrorMessage'

export default function LoginPage() {
  const { user, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  if (user) return <Navigate to={user.roles.includes('Admin') ? '/admin' : '/dashboard'} replace />
  async function submit(event) {
    event.preventDefault()
    setBusy(true)
    setError('')
    const values = Object.fromEntries(new FormData(event.currentTarget))
    try {
      const account = await login(values)
      navigate(account.roles.includes('Admin') ? '/admin' : '/dashboard', { replace: true })
    } catch (failure) { setError(failure.message) }
    finally { setBusy(false) }
  }
  return <AuthLayout title="Welcome back" description="Sign in to your Appointly account.">
    {location.state?.registered && <p role="status" className="mb-5 rounded-xl bg-emerald-50 p-4 text-sm text-emerald-800">Account created. Sign in to continue.</p>}
    <form onSubmit={submit} className="space-y-5">
      <ErrorMessage>{error}</ErrorMessage>
      <Input label="Email" name="email" type="email" autoComplete="username" required maxLength={254} />
      <Input label="Password" name="password" type="password" autoComplete="current-password" required maxLength={128} />
      <Button type="submit" disabled={busy} className="w-full">{busy ? 'Signing in…' : 'Sign in'}</Button>
    </form>
    <p className="mt-6 text-sm text-slate-600">New to Appointly? <Link to="/register" className="font-semibold text-blue-700 hover:underline">Create an account</Link></p>
  </AuthLayout>
}

