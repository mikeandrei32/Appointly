import { useState } from 'react'
import { useApiData } from '../hooks/useApiData'
import { blockDate, unblockDate } from '../services/admin'
import { clinicDate } from '../utils/clinicTime'
import ScheduleEditor from '../components/ScheduleEditor'
import Button from '../components/Button'
import Input from '../components/Input'
import ErrorMessage from '../components/ErrorMessage'
import ConfirmDialog from '../components/ConfirmDialog'

export default function AdminSchedulePage() {
  const schedules = useApiData('/admin/schedules')
  const blocked = useApiData('/admin/blocked-dates')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [selected, setSelected] = useState(null)
  const [removeError, setRemoveError] = useState('')
  async function add(event) {
    event.preventDefault()
    const form = event.currentTarget
    const values = Object.fromEntries(new FormData(form))
    setBusy(true); setError(''); setNotice('')
    try { await blockDate(values); form.reset(); blocked.reload(); setNotice('Date blocked.') }
    catch (failure) { setError(failure.message) }
    finally { setBusy(false) }
  }
  async function remove() {
    if (!selected || busy) return
    setBusy(true); setRemoveError('')
    try { await unblockDate(selected.id); setSelected(null); blocked.reload(); setNotice('Date unblocked. Normal weekly hours apply.') }
    catch (failure) { setRemoveError(failure.message) }
    finally { setBusy(false) }
  }
  return <>
    <h1 className="text-3xl font-bold">Clinic availability</h1>
    <p className="text-sm leading-6 text-slate-600">Hours use Asia/Manila and 30-minute boundaries. Changes that invalidate active future or ongoing appointments are rejected. Manage those appointments before changing availability.</p>
    {notice && <p role="status" className="rounded-xl bg-emerald-50 p-4 text-sm text-emerald-800">{notice}</p>}
    <h2 className="text-xl font-semibold">Weekly hours</h2>
    <ErrorMessage>{schedules.error}</ErrorMessage>
    {schedules.error && <Button onClick={schedules.reload}>Retry hours</Button>}
    {!schedules.data && !schedules.error && <p role="status">Loading weekly hours…</p>}
    <div className="grid gap-4 md:grid-cols-2">{schedules.data?.map((schedule) =>
      <ScheduleEditor key={schedule.id} schedule={schedule} onSaved={() => setNotice(`${schedule.dayOfWeek} hours saved.`)} />)}</div>
    <h2 className="text-xl font-semibold">Blocked dates</h2>
    <form onSubmit={add} className="grid items-end gap-4 rounded-xl border border-slate-200 bg-white p-5 md:grid-cols-3">
      <Input label="Date" type="date" name="date" min={clinicDate()} max="2100-12-31" required disabled={busy} />
      <Input label="Reason (optional)" name="reason" maxLength={200} disabled={busy} />
      <Button type="submit" disabled={busy}>{busy ? 'Saving…' : 'Block date'}</Button>
    </form>
    <ErrorMessage>{error || blocked.error}</ErrorMessage>
    {blocked.error && <Button onClick={blocked.reload}>Retry blocked dates</Button>}
    {!blocked.data && !blocked.error && <p role="status">Loading blocked dates…</p>}
    {blocked.data?.length === 0 && <p className="text-sm text-slate-600">No blocked dates.</p>}
    <ul className="space-y-3">{blocked.data?.map((item) => <li key={item.id} className="flex flex-wrap items-center justify-between gap-4 rounded-xl border border-slate-200 bg-white p-5">
      <div className="min-w-0 flex-1 [overflow-wrap:anywhere]"><p className="font-semibold">{item.date}</p><p className="mt-1 break-words text-sm text-slate-600">{item.reason || 'Clinic unavailable'}</p></div>
      <button type="button" disabled={busy} onClick={() => { setSelected(item); setRemoveError('') }} className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium">Unblock date</button>
    </li>)}</ul>
    <ConfirmDialog open={selected !== null} title="Unblock this date?" busy={busy} onClose={() => setSelected(null)}
      onConfirm={remove} confirmLabel="Unblock" dismissLabel="Keep blocked" busyLabel="Saving…">
      <p>{selected?.date}. Normal weekly hours will apply and patients may book available slots.</p>
      <ErrorMessage>{removeError}</ErrorMessage>
    </ConfirmDialog>
  </>
}
