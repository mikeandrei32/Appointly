import { Link } from 'react-router-dom'
import { useApiData } from '../hooks/useApiData'
import Button from '../components/Button'
import ErrorMessage from '../components/ErrorMessage'
import AdminAppointmentTable from '../components/AdminAppointmentTable'

export default function AdminDashboardPage() {
  const { data, error, reload } = useApiData('/admin/dashboard')
  return <>
    <div className="flex flex-wrap items-center justify-between gap-4"><h1 className="text-3xl font-bold">Clinic overview</h1><Button onClick={reload}>Refresh</Button></div>
    <div className="flex flex-wrap gap-5 text-sm font-semibold text-blue-700"><Link to="/admin/appointments">Manage appointments</Link><Link to="/admin/schedule">Clinic availability</Link></div>
    <ErrorMessage>{error}</ErrorMessage>
    {!data && !error && <p role="status">Loading clinic statistics…</p>}
    {data && <>
      <p className="text-sm text-slate-600">Today is {data.todayDate} in Asia/Manila. Totals include all appointment statuses.</p>
      <dl className="grid grid-cols-2 gap-4 md:grid-cols-3">
        {[['Total appointments', data.total], ['Pending', data.pending], ['Confirmed', data.confirmed], ['Completed', data.completed], ['Cancelled', data.cancelled], ["Today's appointments", data.today]].map(([label, value]) =>
          <div key={label} className="rounded-xl border border-slate-200 bg-white p-5"><dt className="text-sm text-slate-500">{label}</dt><dd className="mt-2 text-3xl font-bold">{value}</dd></div>)}
      </dl>
      <h2 className="text-xl font-semibold">Recently booked</h2>
      <AdminAppointmentTable items={data.recentAppointments} />
    </>}
  </>
}
