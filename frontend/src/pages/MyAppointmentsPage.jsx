import { useEffect, useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import Button from '../components/Button'
import ErrorMessage from '../components/ErrorMessage'
import StatusBadge from '../components/StatusBadge'
import ConfirmDialog from '../components/ConfirmDialog'
import { cancelAppointment, getMyAppointments } from '../services/appointments'
import { formatAppointment } from '../utils/clinicTime'

export default function MyAppointmentsPage() {
  const location = useLocation()
  const [items, setItems] = useState(null)
  const [error, setError] = useState('')
  const [attempt, setAttempt] = useState(0)
  const [selected, setSelected] = useState(null)
  const [busy, setBusy] = useState(false)
  const [cancelError, setCancelError] = useState('')
  const [notice, setNotice] = useState(location.state?.booked ? 'Appointment booked. It is awaiting confirmation.' : '')

  useEffect(() => {
    const controller = new AbortController()
    getMyAppointments(controller.signal).then(setItems).catch((failure) => {
      if (failure.name !== 'AbortError') setError(failure.message)
    })
    return () => controller.abort()
  }, [attempt])

  function refresh() { setError(''); setItems(null); setAttempt((value) => value + 1) }
  async function cancel() {
    if (busy || !selected) return
    setBusy(true)
    setCancelError('')
    try {
      const updated = await cancelAppointment(selected.id)
      setItems((current) => current.map((item) => item.id === updated.id ? updated : item))
      setSelected(null)
      setNotice('Appointment cancelled. The time is now available for another booking.')
    } catch (failure) { setCancelError(failure.message) }
    finally { setBusy(false) }
  }
  return <>
    <div className="flex flex-wrap items-center justify-between gap-4">
      <h1 className="text-3xl font-bold">My appointments</h1>
      <Link to="/book" className="rounded-xl bg-blue-600 px-5 py-3 font-semibold text-white hover:bg-blue-700">Book appointment</Link>
    </div>
    <p className="text-sm text-slate-600">All appointment times are shown in Asia/Manila.</p>
    {notice && <p role="status" className="rounded-xl bg-emerald-50 p-4 text-sm text-emerald-800">{notice}</p>}
    <ErrorMessage>{error}</ErrorMessage>
    {error && <Button onClick={refresh}>Retry</Button>}
    {!items && !error && <p role="status">Loading your appointments…</p>}
    {items?.length === 0 && <p className="rounded-2xl border border-slate-200 bg-white p-8 text-slate-600">You have no appointments yet. Choose “Book appointment” to plan your first visit.</p>}
    <div className="space-y-4">
      {items?.map((item) => <article key={item.id} className="rounded-2xl border border-slate-200 bg-white p-6">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div><h2 className="font-semibold">{formatAppointment(item.appointmentDateTimeUtc)}</h2>
            <p className="mt-2 text-sm text-slate-500">Appointment #{item.id} · {item.durationMinutes} minutes</p>
          </div><StatusBadge status={item.status} />
        </div>
        {item.reasonForVisit && <p className="mt-4 break-words text-sm text-slate-600">{item.reasonForVisit}</p>}
        {item.canCancel && <button type="button" onClick={() => { setSelected(item); setCancelError('') }}
          className="mt-5 rounded-lg border border-slate-300 px-4 py-2 text-sm font-semibold hover:bg-slate-50">Cancel appointment</button>}
      </article>)}
    </div>
    <ConfirmDialog open={selected !== null} title="Cancel this appointment?" busy={busy}
      onClose={() => setSelected(null)} onConfirm={cancel} confirmLabel="Yes, cancel">
      <p>{selected && formatAppointment(selected.appointmentDateTimeUtc)} · Manila time</p>
      <p>This releases the slot. You will need to make a new booking if you change your mind.</p>
      <ErrorMessage>{cancelError}</ErrorMessage>
    </ConfirmDialog>
  </>
}
