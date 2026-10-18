import { CalendarDays } from 'lucide-react'

export default function Brand() {
  return <a href="/" className="inline-flex items-center gap-3 text-xl font-bold tracking-tight" aria-label="Appointly home">
    <span className="rounded-xl bg-blue-600 p-2 text-white"><CalendarDays size={23} aria-hidden="true" /></span>
    Appointly
  </a>
}
