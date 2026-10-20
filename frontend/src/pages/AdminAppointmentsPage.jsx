import { useState } from 'react'
import { useApiData } from '../hooks/useApiData'
import { changeAppointmentStatus } from '../services/admin'
import Button from '../components/Button'
import Input from '../components/Input'
import ErrorMessage from '../components/ErrorMessage'
import ConfirmDialog from '../components/ConfirmDialog'
import AdminAppointmentTable from '../components/AdminAppointmentTable'
import { formatAppointment } from '../utils/clinicTime'

export default function AdminAppointmentsPage() {
  const [filters, setFilters] = useState({ patient: '', date: '', status: '' })
  const [page, setPage] = useState(1)
  const params = new URLSearchParams({ ...filters, page: String(page) })
  const { data, error, reload } = useApiData(`/admin/appointments?${params}`)
  const [action, setAction] = useState(null)
  const [busy, setBusy] = useState(false)
  const [actionError, setActionError] = useState('')
  const [notice, setNotice] = useState('')
  function filter(event) {
    event.preventDefault()
    setFilters(Object.fromEntries(new FormData(event.currentTarget)))
    setPage(1); reload()
  }
  function close() { setAction(null); reload() }
  async function confirm() {
    if (!action || busy) return
    setBusy(true); setActionError('')
    try {
      await changeAppointmentStatus(action.item.id, action.status, action.item.status)
      setNotice(`Appointment #${action.item.id} is now ${action.status}.`)
      close()
    } catch (failure) { setActionError(failure.message) }
    finally { setBusy(false) }
  }
  return <>
    <h1 className="text-3xl font-bold">Appointment management</h1>
    <form onSubmit={filter} className="grid items-end gap-4 rounded-xl border border-slate-200 bg-white p-5 sm:grid-cols-2 lg:grid-cols-4">
      <Input label="Patient name or email" name="patient" maxLength={100} />
      <Input label="Clinic date" name="date" type="date" min="2000-01-01" max="2100-12-31" />
      <label className="block text-sm font-medium">Status<select name="status" className="mt-2 w-full rounded-xl border border-slate-300 bg-white p-3">
        <option value="">All statuses</option>{['Pending', 'Confirmed', 'Completed', 'Cancelled'].map((status) => <option key={status}>{status}</option>)}
      </select></label>
      <Button type="submit">Apply filters</Button>
    </form>
    {notice && <p role="status" className="rounded-xl bg-emerald-50 p-4 text-sm text-emerald-800">{notice}</p>}
    <ErrorMessage>{error}</ErrorMessage>
    {error && <Button onClick={reload}>Retry</Button>}
    {!data && !error && <p role="status">Loading appointments…</p>}
    {data && <>
      <p className="text-sm text-slate-500">{data.total} matching appointments · Asia/Manila</p>
      <AdminAppointmentTable items={data.items} onAction={(item, status) => { setAction({ item, status }); setActionError('') }} />
      <div className="flex flex-wrap items-center gap-4">
        <Button disabled={page === 1} onClick={() => setPage((value) => value - 1)}>Previous</Button>
        <span className="text-sm">Page {page} of {Math.max(1, Math.ceil(data.total / data.pageSize))}</span>
        <Button disabled={page * data.pageSize >= data.total} onClick={() => setPage((value) => value + 1)}>Next</Button>
      </div>
    </>}
    <ConfirmDialog open={action !== null} title={`Mark appointment ${action?.status || ''}?`}
      busy={busy} onClose={close} onConfirm={confirm} confirmLabel="Confirm change" dismissLabel="Back" busyLabel="Saving…">
      {action && <p>{action.item.patientName} · {formatAppointment(action.item.appointmentDateTimeUtc)} (Manila)</p>}
      {['Completed', 'Cancelled'].includes(action?.status) && <p>This is a final status. The appointment cannot be reactivated.</p>}
      <ErrorMessage>{actionError}</ErrorMessage>
    </ConfirmDialog>
  </>
}
