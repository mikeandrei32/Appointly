import { api } from './api'

export const getAvailability = (date, signal) =>
  api(`/availability?date=${encodeURIComponent(date)}`, { signal })
export const getMyAppointments = (signal) => api('/appointments/my', { signal })
export const bookAppointment = (appointmentDateTimeUtc, reasonForVisit) =>
  api('/appointments', { method: 'POST', body: { appointmentDateTimeUtc, reasonForVisit } })
export const cancelAppointment = (id) => api(`/appointments/${id}/cancel`, { method: 'PATCH' })
