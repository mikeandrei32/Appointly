const colors = {
  Pending: 'bg-amber-50 text-amber-800',
  Confirmed: 'bg-blue-50 text-blue-800',
  Completed: 'bg-emerald-50 text-emerald-800',
  Cancelled: 'bg-slate-100 text-slate-600',
}

export default function StatusBadge({ status }) {
  return <span className={`inline-flex rounded-full px-3 py-1 text-xs font-semibold ${colors[status] || colors.Cancelled}`}>{status}</span>
}
