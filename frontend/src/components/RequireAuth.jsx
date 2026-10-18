import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import Button from './Button'
import ErrorMessage from './ErrorMessage'

export default function RequireAuth({ role }) {
  const { user, loading, error, refresh } = useAuth()
  if (loading) return <p role="status" className="p-8">Checking your session…</p>
  if (error) return <div className="mx-auto max-w-lg space-y-4 p-8"><ErrorMessage>{error}</ErrorMessage><Button onClick={refresh}>Retry</Button></div>
  if (!user) return <Navigate to="/login" replace />
  if (role && !user.roles.includes(role)) return <Navigate to="/account" replace />
  return <Outlet />
}

