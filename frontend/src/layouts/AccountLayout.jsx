import { useState } from 'react'
import { Link, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import Brand from '../components/Brand'
import Button from '../components/Button'
import ErrorMessage from '../components/ErrorMessage'

export default function AccountLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  async function signOut() {
    setBusy(true)
    setError('')
    try { await logout(); navigate('/login', { replace: true }) }
    catch (failure) { setError(failure.message) }
    finally { setBusy(false) }
  }
  return <>
    <header className="border-b border-slate-200 bg-white">
      <nav aria-label="Account navigation" className="mx-auto flex max-w-5xl flex-wrap items-center justify-between gap-5 px-6 py-5">
        <Brand /><div className="flex flex-wrap items-center gap-5 text-sm font-medium">
          <Link to="/account" className="text-blue-700">My profile</Link>
          {user.roles.includes('Patient') && <>
            <Link to="/book" className="text-blue-700">Book appointment</Link>
            <Link to="/appointments" className="text-blue-700">My appointments</Link>
          </>}
          {user.roles.includes('Admin') && <>
            <Link to="/admin" className="text-blue-700">Overview</Link>
            <Link to="/admin/appointments" className="text-blue-700">Appointments</Link>
            <Link to="/admin/schedule" className="text-blue-700">Availability</Link>
          </>}
          <Button onClick={signOut} disabled={busy}>{busy ? 'Signing out…' : 'Sign out'}</Button>
        </div>
      </nav>
    </header>
    <main className="mx-auto max-w-5xl space-y-6 px-6 py-12"><ErrorMessage>{error}</ErrorMessage><Outlet /></main>
  </>
}

