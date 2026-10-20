import { mkdir } from 'node:fs/promises'
import { fileURLToPath } from 'node:url'
import { test, expect } from '@playwright/test'

const output = fileURLToPath(new URL('../../docs/screenshots/', import.meta.url))
const patient = { id: 'demo-patient', fullName: 'Alex Reyes', email: 'alex@example.test', roles: ['Patient'] }
const visits = [
  { id: 101, appointmentDateTimeUtc: '2030-01-07T01:00:00Z', durationMinutes: 30, status: 'Confirmed',
    reasonForVisit: 'Routine consultation', canCancel: true, patientName: 'Alex Reyes', patientEmail: 'alex@example.test' },
  { id: 102, appointmentDateTimeUtc: '2030-01-08T02:30:00Z', durationMinutes: 30, status: 'Pending',
    reasonForVisit: 'Follow-up visit', canCancel: true, patientName: 'Sam Cruz', patientEmail: 'sam@example.test' },
]

test('capture implemented pages with fictional documentation fixtures', async ({ page }) => {
  await mkdir(output, { recursive: true })
  await page.clock.setFixedTime(new Date('2030-01-06T00:00:00Z'))
  let user = patient
  await page.route('**/api/**', async (route) => {
    const url = new URL(route.request().url())
    const json = (body) => route.fulfill({ json: body })
    switch (url.pathname) {
      case '/api/auth/me': return json(user)
      case '/api/appointments/my/overview':
        return json({ upcoming: [visits[0]], history: [] })
      case '/api/availability': return json({
        date: url.searchParams.get('date'),
        slots: ['09:00', '09:30', '10:00', '10:30', '11:00', '11:30'].map((time, index) => ({
          localTime: time, startUtc: new Date(Date.UTC(2030, 0, 7, 1, index * 30)).toISOString(),
        })),
      })
      case '/api/admin/appointments': return json({ items: visits, total: visits.length, page: 1, pageSize: 20 })
      default: throw new Error(`Unexpected screenshot API request: ${url.pathname}`)
    }
  })

  await page.setViewportSize({ width: 1440, height: 1000 })
  await page.goto('/dashboard')
  await expect(page.getByRole('heading', { name: 'Welcome, Alex Reyes' })).toBeVisible()
  await expect(page.getByRole('article')).toContainText('Confirmed')
  await page.screenshot({ path: `${output}/patient-dashboard.png`, fullPage: true })

  await page.setViewportSize({ width: 390, height: 844 })
  await page.goto('/book')
  await page.getByLabel('Appointment date').fill('2030-01-07')
  await page.getByRole('radio', { name: '10:00', exact: true }).check()
  await page.getByLabel('Reason for visit').fill('Routine consultation')
  await expect(page.getByRole('button', { name: 'Confirm booking' })).toBeEnabled()
  await page.getByRole('heading', { name: 'Book an appointment' }).scrollIntoViewIfNeeded()
  await page.screenshot({ path: `${output}/mobile-booking.png`, fullPage: true })

  user = { id: 'demo-admin', fullName: 'Clinic Administrator', email: 'admin@example.test', roles: ['Admin'] }
  await page.setViewportSize({ width: 1440, height: 900 })
  await page.goto('/admin/appointments')
  await expect(page.getByRole('table')).toContainText('Alex Reyes')
  await expect(page.getByRole('table')).toContainText('Sam Cruz')
  await page.screenshot({ path: `${output}/admin-appointments.png`, fullPage: true })
})
