import { useId } from 'react'

export default function Input({ label, hint, ...props }) {
  const id = useId()
  return <div>
    <label htmlFor={id} className="mb-2 block text-sm font-medium text-slate-700">{label}</label>
    <input id={id} aria-describedby={hint ? `${id}-hint` : undefined} className="w-full rounded-xl border border-slate-300 bg-white px-4 py-3 text-slate-900" {...props} />
    {hint && <p id={`${id}-hint`} className="mt-2 text-xs leading-5 text-slate-500">{hint}</p>}
  </div>
}
