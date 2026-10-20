import { useState } from 'react'
import Button from './Button'
import Input from './Input'
import ErrorMessage from './ErrorMessage'
import { updateSchedule } from '../services/admin'

export default function ScheduleEditor({ schedule, onSaved }) {
  const [isOpen, setIsOpen] = useState(schedule.isOpen)
  const [openingTime, setOpeningTime] = useState(schedule.openingTime)
  const [closingTime, setClosingTime] = useState(schedule.closingTime)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  async function save(event) {
    event.preventDefault()
    setBusy(true); setError(''); setNotice('')
    try {
      await updateSchedule(schedule.id, { isOpen, openingTime, closingTime })
      setNotice('Hours saved.'); onSaved()
    } catch (failure) { setError(failure.message) }
    finally { setBusy(false) }
  }
  return <form onSubmit={save} className="space-y-4 rounded-xl border border-slate-200 bg-white p-5">
    <div className="flex items-center justify-between gap-3"><h3 className="font-semibold">{schedule.dayOfWeek}</h3>
      <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={isOpen} disabled={busy} onChange={(event) => setIsOpen(event.target.checked)} />Open</label>
    </div>
    <div className="grid grid-cols-2 gap-3">
      <Input label="Opens" type="time" step={1800} value={openingTime} required disabled={busy || !isOpen} onChange={(event) => setOpeningTime(event.target.value)} />
      <Input label="Closes" type="time" step={1800} value={closingTime} required disabled={busy || !isOpen} onChange={(event) => setClosingTime(event.target.value)} />
    </div>
    <ErrorMessage>{error}</ErrorMessage>
    {notice && <p role="status" className="text-sm text-emerald-700">{notice}</p>}
    <Button type="submit" disabled={busy}>{busy ? 'Saving…' : 'Save hours'}</Button>
  </form>
}
