import { useEffect, useId, useRef } from 'react'
import Button from './Button'

export default function ConfirmDialog({ open, title, children, busy, onClose, onConfirm, confirmLabel,
  dismissLabel = 'Keep appointment', busyLabel = 'Cancelling…' }) {
  const dialog = useRef(null)
  const titleId = useId()
  useEffect(() => {
    const element = dialog.current
    if (open && !element.open) element.showModal()
    if (!open && element.open) element.close()
    return () => { if (element.open) element.close() }
  }, [open])
  return <dialog ref={dialog} aria-labelledby={titleId}
    onCancel={(event) => { event.preventDefault(); if (!busy) onClose() }}
    className="m-auto w-[calc(100%-2rem)] max-w-md rounded-2xl border border-slate-200 bg-white p-6 text-slate-900 shadow-xl backdrop:bg-slate-900/40">
    <h2 id={titleId} className="text-xl font-semibold">{title}</h2>
    <div className="my-5 space-y-3 text-sm leading-6 text-slate-600">{children}</div>
    <div className="flex flex-wrap justify-end gap-3">
      <button type="button" disabled={busy} onClick={onClose} className="rounded-xl border border-slate-300 px-4 py-3 font-medium disabled:opacity-50">{dismissLabel}</button>
      <Button type="button" disabled={busy} onClick={onConfirm}>{busy ? busyLabel : confirmLabel}</Button>
    </div>
  </dialog>
}
