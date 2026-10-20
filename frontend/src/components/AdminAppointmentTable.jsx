import StatusBadge from './StatusBadge'
import { formatAppointment } from '../utils/clinicTime'

export default function AdminAppointmentTable({ items, onAction }) {
  if (!items.length) return <p className="rounded-xl bg-white p-6 text-slate-600">No appointments match this view.</p>
  return <div role="region" aria-label="Appointments table" tabIndex={0} className="overflow-x-auto rounded-xl border border-slate-200 bg-white">
    <table className="w-full min-w-[680px] text-left text-sm">
      <thead className="bg-slate-50 text-slate-600"><tr>
        <th scope="col" className="p-4">Patient</th><th scope="col" className="p-4">Visit · Manila time</th>
        <th scope="col" className="p-4">Status</th>{onAction && <th scope="col" className="p-4">Actions</th>}
      </tr></thead>
      <tbody className="divide-y divide-slate-100">{items.map((item) => <tr key={item.id}>
        <td className="max-w-64 break-words p-4"><p className="font-semibold">{item.patientName}</p><p className="mt-1 break-all text-xs text-slate-500">{item.patientEmail}</p></td>
        <td className="p-4"><p>{formatAppointment(item.appointmentDateTimeUtc)}</p><p className="mt-1 text-xs text-slate-500">#{item.id} · {item.durationMinutes} minutes</p>
          {item.reasonForVisit && <details className="mt-2 max-w-60"><summary className="cursor-pointer text-blue-700">Reason for visit</summary><p className="mt-2 break-words text-slate-600">{item.reasonForVisit}</p></details>}
        </td>
        <td className="p-4"><StatusBadge status={item.status} /></td>
        {onAction && <td className="p-4"><div className="flex flex-wrap gap-2">
          {item.status === 'Pending' && <Action label="Confirm" onClick={() => onAction(item, 'Confirmed')} />}
          {item.status === 'Confirmed' && <Action label="Complete" onClick={() => onAction(item, 'Completed')} />}
          {['Pending', 'Confirmed'].includes(item.status) && <Action label="Cancel" onClick={() => onAction(item, 'Cancelled')} />}
          {['Completed', 'Cancelled'].includes(item.status) && <span className="text-slate-500">Historical record</span>}
        </div></td>}
      </tr>)}</tbody>
    </table>
  </div>
}

function Action({ label, onClick }) {
  return <button type="button" onClick={onClick} className="rounded-lg border border-slate-300 px-3 py-2 font-medium hover:bg-slate-50">{label}</button>
}
