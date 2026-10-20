import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import Button from '../components/Button'
import Input from '../components/Input'
import ErrorMessage from '../components/ErrorMessage'
import TimeSlotPicker from '../components/TimeSlotPicker'
import { bookAppointment, getAvailability } from '../services/appointments'
import { clinicDate, formatAppointment } from '../utils/clinicTime'

export default function BookAppointmentPage() {
  const navigate = useNavigate()
  const [date, setDate] = useState(() => clinicDate())
  const [availability, setAvailability] = useState(null)
  const [selected, setSelected] = useState('')
  const [reason, setReason] = useState('')
  const [error, setError] = useState('')
  const [bookingError, setBookingError] = useState('')
  const [attempt, setAttempt] = useState(0)
  const [busy, setBusy] = useState(false)
  const slots = availability?.date === date ? availability.slots : null

  useEffect(() => {
    if (!date) return
    const controller = new AbortController()
    getAvailability(date, controller.signal).then(setAvailability).catch((failure) => {
      if (failure.name !== 'AbortError') setError(failure.message)
    })
    return () => controller.abort()
  }, [date, attempt])

  function refresh(nextDate = date) {
    setDate(nextDate)
    setAvailability(null)
    setSelected('')
    setError('')
    setAttempt((value) => value + 1)
  }

  async function submit(event) {
    event.preventDefault()
    if (!selected || busy || !slots?.some((slot) => slot.startUtc === selected)) return
    setBusy(true)
    setBookingError('')
    try {
      await bookAppointment(selected, reason.trim() || null)
      navigate('/appointments', { state: { booked: true } })
    } catch (failure) {
      setBookingError(failure.message)
      // Even a previously displayed slot can expire or be taken by another patient.
      refresh()
    } finally { setBusy(false) }
  }

  return <>
    <div><p className="text-sm font-semibold text-blue-700">Plan your visit</p>
      <h1 className="mt-2 text-3xl font-bold">Book an appointment</h1>
      <p className="mt-3 text-sm text-slate-600">Visits last 30 minutes. All dates and times use Asia/Manila.</p>
    </div>
    <form onSubmit={submit} className="grid gap-6 lg:grid-cols-[1.5fr_1fr]">
      <section className="space-y-6 rounded-2xl border border-slate-200 bg-white p-6">
        <Input label="Appointment date" type="date" min={clinicDate()} max="2100-12-31" required
          value={date} onChange={(event) => { refresh(event.target.value); setBookingError('') }} disabled={busy} />
        <ErrorMessage>{error}</ErrorMessage>
        {error && <Button type="button" onClick={() => refresh()} disabled={busy}>Retry availability</Button>}
        {!date ? <p className="text-sm text-slate-600">Choose a date to view available times.</p>
          : !slots && !error ? <p role="status" className="text-sm text-slate-600">Loading available times…</p>
          : slots?.length === 0 ? <p role="status" className="rounded-xl bg-slate-50 p-4 text-sm text-slate-600">No times are available on this date. Try another date.</p>
          : slots && <TimeSlotPicker slots={slots} selected={selected} onChange={setSelected} disabled={busy} />}
        <div><label htmlFor="reason" className="mb-2 block text-sm font-medium">Reason for visit <span className="text-slate-500">(optional)</span></label>
          <textarea id="reason" maxLength={200} rows={3} value={reason} disabled={busy}
            onChange={(event) => setReason(event.target.value)} aria-describedby="reason-hint"
            className="w-full resize-y rounded-xl border border-slate-300 p-3" />
          <p id="reason-hint" className="mt-2 text-xs text-slate-500">Keep it brief. Do not include medical history or sensitive information. {reason.length}/200</p>
        </div>
      </section>
      <section className="h-fit space-y-5 rounded-2xl border border-slate-200 bg-white p-6">
        <h2 className="text-lg font-semibold">Appointment summary</h2>
        <p className="text-sm text-slate-600">{selected ? formatAppointment(selected) : 'Choose a date and an available time.'}</p>
        <p className="text-sm text-slate-600">30-minute visit · Manila time</p>
        <p className="text-sm text-slate-600">Your appointment will be Pending until an administrator confirms it. You can cancel before the visit starts.</p>
        <ErrorMessage>{bookingError}</ErrorMessage>
        <Button type="submit" className="w-full" disabled={!selected || busy}>{busy ? 'Booking…' : 'Confirm booking'}</Button>
      </section>
    </form>
  </>
}
