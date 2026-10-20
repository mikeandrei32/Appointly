import StatusBadge from './StatusBadge'
import { formatAppointment } from '../utils/clinicTime'

export default function PatientAppointmentCard({ item, onCancel }) {
  return <article className="rounded-2xl border border-slate-200 bg-white p-6">
    <div className="flex flex-wrap items-start justify-between gap-4">
      <div>
        <h2 className="font-semibold">{formatAppointment(item.appointmentDateTimeUtc)}</h2>
        <p className="mt-2 text-sm text-slate-500">Appointment #{item.id} · {item.durationMinutes} minutes · Manila time</p>
      </div>
      <StatusBadge status={item.status} />
    </div>
    {item.reasonForVisit && <p className="mt-4 break-words text-sm text-slate-600">{item.reasonForVisit}</p>}
    {item.cancelledAtUtc && <p className="mt-3 text-sm text-slate-500">Cancelled {formatAppointment(item.cancelledAtUtc)} (Manila)</p>}
    {item.canCancel && onCancel && <button type="button" onClick={() => onCancel(item)}
      className="mt-5 rounded-lg border border-slate-300 px-4 py-2 text-sm font-semibold hover:bg-slate-50">Cancel appointment</button>}
  </article>
}
