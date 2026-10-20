import { useState } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import Brand from '../components/Brand'
import Button from '../components/Button'
import ErrorMessage from '../components/ErrorMessage'

const navClass = ({ isActive }) => isActive ? 'font-bold text-blue-800 underline underline-offset-4' : 'text-blue-700 hover:underline'

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
          <NavLink to="/account" className={navClass}>My profile</NavLink>
          {user.roles.includes('Patient') && <>
            <NavLink to="/dashboard" className={navClass}>Dashboard</NavLink>
            <NavLink to="/book" className={navClass}>Book appointment</NavLink>
            <NavLink end to="/appointments" className={navClass}>Upcoming</NavLink>
            <NavLink to="/appointments/history" className={navClass}>History</NavLink>
          </>}
          {user.roles.includes('Admin') && <>
            <NavLink end to="/admin" className={navClass}>Overview</NavLink>
            <NavLink to="/admin/appointments" className={navClass}>Appointments</NavLink>
            <NavLink to="/admin/schedule" className={navClass}>Availability</NavLink>
          </>}
          <Button onClick={signOut} disabled={busy}>{busy ? 'Signing out…' : 'Sign out'}</Button>
        </div>
      </nav>
    </header>
    <main className="mx-auto max-w-5xl space-y-6 px-6 py-12"><ErrorMessage>{error}</ErrorMessage><Outlet /></main>
  </>
}

