import { api } from './api'

export const changeAppointmentStatus = (id, status, expectedStatus) =>
  api(`/admin/appointments/${id}/status`, { method: 'PATCH', body: { status, expectedStatus } })
export const updateSchedule = (id, values) => api(`/admin/schedules/${id}`, { method: 'PUT', body: values })
export const blockDate = (values) => api('/admin/blocked-dates', { method: 'POST', body: values })
export const unblockDate = (id) => api(`/admin/blocked-dates/${id}`, { method: 'DELETE' })
