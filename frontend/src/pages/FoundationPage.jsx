import { useEffect, useState } from 'react'
import { ArrowUpRight, CheckCircle2, RefreshCw } from 'lucide-react'
import Brand from '../components/Brand'
import { Link } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import { getHealth } from '../services/health'

export default function FoundationPage() {
  const { user } = useAuth()
  const [attempt, setAttempt] = useState(0)
  const [health, setHealth] = useState(null)
  const [error, setError] = useState('')

  useEffect(() => {
    const controller = new AbortController()
    getHealth(controller.signal).then(setHealth).catch((failure) => {
      if (failure.name !== 'AbortError') setError(failure.message)
    })
    return () => controller.abort()
  }, [attempt])

  function retry() {
    setHealth(null)
    setError('')
    setAttempt((value) => value + 1)
  }

  return (
    <div className="min-h-screen">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-4 px-6 py-5">
          <Brand />
          <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">Development · Phase 3</span>
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-6 py-14 md:py-24">
        <div className="grid items-start gap-12 lg:grid-cols-2">
          <section>
            <p className="mb-4 text-sm font-semibold uppercase tracking-widest text-blue-700">Clinic appointment booking</p>
            <h1 className="max-w-xl text-4xl font-bold leading-tight tracking-tight md:text-5xl">A simpler way to plan your clinic visit.</h1>
            <p className="mt-6 max-w-lg text-lg leading-8 text-slate-600">Appointly is taking shape. The application foundation connects this React interface to an ASP.NET Core API and a SQLite database.</p>
            <p className="mt-4 text-sm leading-6 text-slate-500">Patient registration, booking, and cancellation are ready. Administrator clinic management is coming in the next phase.</p>
            <div className="mt-6 flex flex-wrap gap-4">{user ? <Link to={user.roles.includes('Admin') ? '/admin' : '/account'} className="font-semibold text-blue-700 underline">Go to my account</Link> : <><Link to="/register" className="font-semibold text-blue-700 underline">Create an account</Link><Link to="/login" className="font-semibold text-blue-700 underline">Sign in</Link></>}</div><a href="/openapi/v1.json" className="mt-8 inline-flex items-center gap-2 rounded-xl bg-blue-600 px-5 py-3 font-semibold text-white hover:bg-blue-700">View API specification <ArrowUpRight size={18} aria-hidden="true" /></a>
          </section>
          <section aria-labelledby="status-heading" className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm md:p-8">
            <div className="flex items-center justify-between gap-4">
              <h2 id="status-heading" className="text-xl font-semibold">Application status</h2>
              <button type="button" onClick={retry} aria-label="Refresh application status" className="rounded-lg border border-slate-200 p-2 text-slate-600 hover:bg-slate-50"><RefreshCw size={18} aria-hidden="true" /></button>
            </div>
            <div role="status" aria-live="polite" className="mt-6">
              {error ? <p className="rounded-xl bg-red-50 p-4 text-sm leading-6 text-red-800">{error}</p> : health ? (
                <div className="flex gap-3 rounded-xl bg-emerald-50 p-4 text-emerald-800">
                  <CheckCircle2 className="shrink-0" size={22} aria-hidden="true" />
                  <div><p className="font-semibold">API and database connected</p><p className="mt-1 text-sm">Live response from /api/health</p></div>
                </div>
              ) : <p className="rounded-xl bg-slate-50 p-4 text-slate-600">Checking API and database…</p>}
            </div>
            <dl className="mt-6 divide-y divide-slate-100 text-sm">
              <div className="flex justify-between gap-3 py-4"><dt className="text-slate-500">Frontend</dt><dd className="font-medium">React + Vite</dd></div>
              <div className="flex justify-between gap-3 py-4"><dt className="text-slate-500">Backend</dt><dd className="font-medium">ASP.NET Core</dd></div>
              <div className="flex justify-between gap-3 py-4"><dt className="text-slate-500">Clinic timezone</dt><dd className="font-medium">{health?.clinicTimezone || 'Asia/Manila'}</dd></div>
            </dl>
            <p className="mt-4 text-xs text-slate-500">Booking and cancellation are ready. Admin management is next.</p>
          </section>
        </div>
      </main>
      <footer className="mx-auto max-w-6xl border-t border-slate-200 px-6 py-6 text-sm text-slate-500">Appointly · Portfolio demonstration · No real patient data</footer>
    </div>
  )
}
