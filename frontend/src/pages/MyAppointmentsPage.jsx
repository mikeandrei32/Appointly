import { useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import Button from '../components/Button'
import ErrorMessage from '../components/ErrorMessage'
import ConfirmDialog from '../components/ConfirmDialog'
import PatientAppointmentCard from '../components/PatientAppointmentCard'
import { useApiData } from '../hooks/useApiData'
import { cancelAppointment } from '../services/appointments'
import { formatAppointment } from '../utils/clinicTime'

export default function MyAppointmentsPage({ history = false }) {
  const location = useLocation()
  const { data, error, reload } = useApiData('/appointments/my/overview')
  const items = data && (history ? data.history : data.upcoming)
  const [selected, setSelected] = useState(null)
  const [busy, setBusy] = useState(false)
  const [cancelError, setCancelError] = useState('')
  const [notice, setNotice] = useState(location.state?.booked ? 'Appointment booked. It is awaiting confirmation.' : '')

  // Preserve the trigger button on ordinary dismissal so dialog focus can return.
  function close() { setSelected(null); if (cancelError) reload() }
  async function cancel() {
    if (busy || !selected) return
    setBusy(true); setCancelError('')
    try {
      await cancelAppointment(selected.id)
      setSelected(null)
      reload()
      setNotice('Appointment cancelled. You can find it in your history.')
    } catch (failure) { setCancelError(failure.message) }
    finally { setBusy(false) }
  }
  return <>
    <div className="flex flex-wrap items-center justify-between gap-4">
      <h1 className="text-3xl font-bold">{history ? 'Appointment history' : 'Upcoming appointments'}</h1>
      <Button onClick={reload}>Refresh</Button>
    </div>
    <div className="flex flex-wrap gap-5 font-semibold text-blue-700">
      <Link to={history ? '/appointments' : '/appointments/history'} className="underline">{history ? 'Upcoming visits' : 'View history'}</Link>
      <Link to="/book" className="underline">Book appointment</Link>
    </div>
    {notice && <p role="status" className="rounded-xl bg-emerald-50 p-4 text-sm text-emerald-800">{notice}</p>}
    <ErrorMessage>{error}</ErrorMessage>
    {error && <Button onClick={reload}>Retry</Button>}
    {!data && !error && <p role="status">Loading your appointments…</p>}
    {items?.length === 0 && <p className="rounded-2xl border border-slate-200 bg-white p-8 text-slate-600">{history
      ? 'No appointment history yet. Past and closed visits will appear here.'
      : 'No upcoming appointments. Choose “Book appointment” to plan your next visit.'}</p>}
    <div className="space-y-4">
      {items?.map((item) => <PatientAppointmentCard key={item.id} item={item}
        onCancel={history ? undefined : (appointment) => { setSelected(appointment); setCancelError('') }} />)}
    </div>
    <ConfirmDialog open={selected !== null} title="Cancel this appointment?" busy={busy}
      onClose={close} onConfirm={cancel} confirmLabel="Yes, cancel">
      <p>{selected && formatAppointment(selected.appointmentDateTimeUtc)} · Manila time</p>
      <p>This releases the slot. You will need to make a new booking if you change your mind.</p>
      <ErrorMessage>{cancelError}</ErrorMessage>
    </ConfirmDialog>
  </>
}
