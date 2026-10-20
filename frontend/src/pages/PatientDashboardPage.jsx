import { Link } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import { useApiData } from '../hooks/useApiData'
import Button from '../components/Button'
import ErrorMessage from '../components/ErrorMessage'
import PatientAppointmentCard from '../components/PatientAppointmentCard'

export default function PatientDashboardPage() {
  const { user } = useAuth()
  const { data, error, reload } = useApiData('/appointments/my/overview')
  return <>
    <div className="flex flex-wrap items-center justify-between gap-4">
      <div className="min-w-0 flex-1 [overflow-wrap:anywhere]"><p className="text-sm font-semibold text-blue-700">Your patient dashboard</p>
        <h1 className="mt-2 break-words text-3xl font-bold">Welcome, {user.fullName}</h1></div>
      <Button onClick={reload}>Refresh</Button>
    </div>
    <div className="flex flex-wrap gap-4">
      <Link to="/book" className="rounded-xl bg-blue-600 px-5 py-3 font-semibold text-white hover:bg-blue-700">Book appointment</Link>
      <Link to="/account" className="rounded-xl border border-slate-300 px-5 py-3 font-semibold">View profile</Link>
    </div>
    <ErrorMessage>{error}</ErrorMessage>
    {error && <Button onClick={reload}>Retry</Button>}
    {!data && !error && <p role="status">Loading your dashboard…</p>}
    {data && <>
      <dl className="grid gap-4 sm:grid-cols-2">
        <div className="rounded-xl border border-slate-200 bg-white p-5"><dt>Upcoming and ongoing visits</dt><dd className="mt-2 text-3xl font-bold">{data.upcoming.length}</dd></div>
        <div className="rounded-xl border border-slate-200 bg-white p-5"><dt>Past and closed appointments</dt><dd className="mt-2 text-3xl font-bold">{data.history.length}</dd></div>
      </dl>
      <section className="space-y-4" aria-labelledby="next-visit">
        <h2 id="next-visit" className="text-xl font-semibold">Your next or ongoing visit</h2>
        {data.upcoming.length > 0 ? <PatientAppointmentCard item={data.upcoming[0]} /> :
          <p className="rounded-xl border border-slate-200 bg-white p-6 text-slate-600">No upcoming visits. Book an appointment when you are ready.</p>}
        <Link to="/appointments" className="inline-block font-semibold text-blue-700 underline">View upcoming visits and cancellation options</Link>
      </section>
      <Link to="/appointments/history" className="inline-block font-semibold text-blue-700 underline">View appointment history</Link>
      <p className="text-sm text-slate-500">Times use Asia/Manila. Refresh to see the latest clinic updates. Only future Pending or Confirmed visits can be cancelled.</p>
    </>}
  </>
}
