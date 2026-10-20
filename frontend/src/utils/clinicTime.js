export const clinicTimezone = 'Asia/Manila'

export function clinicDate(value = new Date()) {
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone: clinicTimezone, year: 'numeric', month: '2-digit', day: '2-digit',
  }).formatToParts(value)
  const part = (name) => parts.find((item) => item.type === name).value
  return `${part('year')}-${part('month')}-${part('day')}`
}

export function formatAppointment(value) {
  return new Intl.DateTimeFormat('en-PH', {
    timeZone: clinicTimezone, dateStyle: 'medium', timeStyle: 'short',
  }).format(new Date(value))
}
