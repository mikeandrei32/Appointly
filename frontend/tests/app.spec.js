import { test, expect } from '@playwright/test'

const patient = { id: 'patient-1', fullName: 'Demo Patient', email: 'patient@example.test', roles: ['Patient'] }
const admin = { ...patient, id: 'admin-1', roles: ['Admin'] }
const visit = {
  id: 1, appointmentDateTimeUtc: '2030-01-07T01:00:00Z', durationMinutes: 30,
  status: 'Pending', reasonForVisit: 'Routine visit', canCancel: true,
  patientName: patient.fullName, patientEmail: patient.email,
}

test.beforeEach(async ({ page }) => {
  await page.clock.setFixedTime(new Date('2030-01-06T00:00:00Z'))
})

// Browser tests exercise UI behavior; the .NET suite tests real auth and SQLite.
async function mockApi(page, { user = patient, appointments = [], overrides = {} } = {}) {
  const state = { user, appointments: structuredClone(appointments) }
  await page.route('**/api/**', async (route) => {
    const request = route.request()
    const url = new URL(request.url())
    const key = `${request.method()} ${url.pathname}`
    const json = (body, status = 200) => route.fulfill({ status, json: body })
    if (overrides[key]) return overrides[key]({ route, request, url, json, state })
    switch (key) {
      case 'GET /api/auth/me': return json(state.user || { title: 'Sign in required' }, state.user ? 200 : 401)
      case 'GET /api/auth/csrf': return json({ token: 'test-token' })
      case 'POST /api/auth/login': state.user = user || patient; return json(state.user)
      case 'POST /api/auth/register': return route.fulfill({ status: 201, json: {} })
      case 'POST /api/auth/logout': state.user = null; return route.fulfill({ status: 204 })
      case 'GET /api/health': return json({ clinicTimezone: 'Asia/Manila' })
      case 'GET /api/appointments/my/overview': return json({
        upcoming: state.appointments.filter((item) => item.status !== 'Cancelled'),
        history: state.appointments.filter((item) => item.status === 'Cancelled'),
      })
      case 'GET /api/availability': return json({
        date: url.searchParams.get('date'), slots: [{ startUtc: visit.appointmentDateTimeUtc, localTime: '09:00' }],
      })
      case 'POST /api/appointments':
        expect(request.headers()['x-csrf-token']).toBe('test-token')
        state.appointments.push({ ...visit, reasonForVisit: request.postDataJSON().reasonForVisit })
        return json(state.appointments[0], 201)
      case 'PATCH /api/appointments/1/cancel':
        state.appointments[0] = { ...state.appointments[0], status: 'Cancelled', canCancel: false, cancelledAtUtc: '2030-01-06T00:00:00Z' }
        return json(state.appointments[0])
      case 'GET /api/admin/appointments':
        return json({ items: state.appointments, total: state.appointments.length, page: 1, pageSize: 20 })
      case 'GET /api/admin/dashboard':
        return json({ total: 1, pending: 1, confirmed: 0, completed: 0, cancelled: 0, today: 1,
          todayDate: '2030-01-07', recentAppointments: state.appointments })
      case 'GET /api/admin/schedules':
        return json([{ id: 2, dayOfWeek: 'Monday', isOpen: true, openingTime: '09:00', closingTime: '17:00' }])
      case 'GET /api/admin/blocked-dates': return json([])
      default: throw new Error(`Unexpected API call: ${key}`)
    }
  })
  return state
}

async function fitsViewport(page) {
  const dimensions = await page.evaluate(() => ({
    width: document.documentElement.clientWidth,
    content: document.documentElement.scrollWidth,
  }))
  expect(dimensions.content).toBeLessThanOrEqual(dimensions.width + 1)
}

test('patient can sign in, book, cancel, view history, and sign out', async ({ page }) => {
  await mockApi(page, { user: null })
  await page.goto('/dashboard')
  await expect(page).toHaveURL(/\/login$/)
  await page.getByLabel('Email', { exact: true }).fill(patient.email)
  await page.getByLabel('Password', { exact: true }).fill('DemoPassword1!')
  await page.getByRole('button', { name: 'Sign in', exact: true }).click()
  await expect(page.getByRole('heading', { name: 'Welcome, Demo Patient' })).toBeVisible()
  await expect(page.getByText('No upcoming visits.', { exact: false })).toBeVisible()
  await page.getByRole('link', { name: 'Book appointment', exact: true }).first().click()
  await page.getByLabel('Appointment date').fill('2030-01-07')
  await page.getByRole('radio', { name: '09:00' }).check()
  await page.getByLabel('Reason for visit').fill('Routine visit')
  await fitsViewport(page)
  await page.getByRole('button', { name: 'Confirm booking' }).click()
  await expect(page.getByText('Appointment booked.', { exact: false })).toBeVisible()
  await expect(page.getByRole('article')).toContainText('9:00')
  const cancel = page.getByRole('button', { name: 'Cancel appointment', exact: true })
  await cancel.click()
  await expect(page.getByRole('dialog')).toBeVisible()
  await page.keyboard.press('Escape')
  await expect(page.getByRole('dialog')).not.toBeVisible()
  await expect(cancel).toBeFocused()
  await cancel.click()
  await page.getByRole('button', { name: 'Yes, cancel' }).click()
  await expect(page.getByText('Appointment cancelled.', { exact: false })).toBeVisible()
  await page.getByRole('link', { name: 'History', exact: true }).click()
  await expect(page.getByRole('article')).toContainText('Cancelled')
  await expect(page.getByRole('button', { name: 'Cancel appointment', exact: true })).toHaveCount(0)
  await fitsViewport(page)
  await page.getByRole('button', { name: 'Sign out' }).click()
  await expect(page).toHaveURL(/\/login$/)
})

test('registration validates matching passwords and redirects to sign in', async ({ page }) => {
  await mockApi(page, { user: null })
  await page.goto('/register')
  await page.getByLabel('Full name', { exact: true }).fill('Demo Patient')
  await page.getByLabel('Email', { exact: true }).fill(patient.email)
  await page.getByLabel('Password', { exact: true }).fill('DemoPassword1!')
  await page.getByLabel('Confirm password', { exact: true }).fill('DifferentPassword1!')
  await page.getByRole('button', { name: 'Create account' }).click()
  await expect(page.getByRole('alert')).toContainText(/match/i)
  await page.getByLabel('Confirm password', { exact: true }).fill('DemoPassword1!')
  await fitsViewport(page)
  await page.getByRole('button', { name: 'Create account' }).click()
  await expect(page).toHaveURL(/\/login$/)
  await expect(page.getByText('Account created.', { exact: false })).toBeVisible()
})

test('admin can block and unblock a date through confirmation', async ({ page }) => {
  let blocked = []
  await mockApi(page, { user: admin, overrides: {
    'GET /api/admin/blocked-dates': ({ json }) => json(blocked),
    'POST /api/admin/blocked-dates': ({ request, json }) => {
      blocked = [{ id: 1, ...request.postDataJSON() }]
      return json(blocked[0], 201)
    },
    'DELETE /api/admin/blocked-dates/1': ({ route }) => {
      blocked = []
      return route.fulfill({ status: 204 })
    },
  } })
  await page.goto('/admin/schedule')
  await page.getByLabel('Date', { exact: true }).fill('2030-01-08')
  await page.getByLabel('Reason (optional)').fill('R'.repeat(200))
  await page.getByRole('button', { name: 'Block date', exact: true }).click()
  await expect(page.getByText('Date blocked.', { exact: true })).toBeVisible()
  await fitsViewport(page)
  await page.getByRole('button', { name: 'Unblock date', exact: true }).click()
  await page.getByRole('dialog').getByRole('button', { name: 'Unblock', exact: true }).click()
  await expect(page.getByText('No blocked dates.', { exact: true })).toBeVisible()
})

test('admin tables contain horizontal scrolling within the table region', async ({ page }) => {
  await mockApi(page, { user: admin, appointments: [{
    ...visit, patientName: 'N'.repeat(100), patientEmail: `${'e'.repeat(100)}@example.test`,
    reasonForVisit: 'R'.repeat(200),
  }] })
  for (const path of ['/admin', '/admin/appointments']) {
    await page.goto(path)
    await expect(page.getByRole('table')).toBeVisible()
    await page.getByText('Reason for visit', { exact: true }).click()
    await fitsViewport(page)
    const region = page.getByRole('region', { name: 'Appointments table' })
    await expect(region).toHaveCSS('overflow-x', 'auto')
  }
})

test('patient routes reject admin-only users and admin routes reject patients', async ({ page }) => {
  const state = await mockApi(page)
  await page.goto('/admin/appointments')
  await expect(page).toHaveURL(/\/account$/)
  state.user = admin
  await page.goto('/appointments/history')
  await expect(page).toHaveURL(/\/account$/)
})

test('dashboard shows loading, failure, retry, and empty states', async ({ page }) => {
  let release
  const gate = new Promise((resolve) => { release = resolve })
  let fail = true
  await mockApi(page, { overrides: {
    'GET /api/appointments/my/overview': async ({ json }) => {
      await gate
      return fail ? json({ detail: 'Temporary outage' }, 503) : json({ upcoming: [], history: [] })
    },
  } })
  await page.goto('/dashboard')
  await expect(page.getByText('Loading your dashboard…')).toBeVisible()
  release()
  await expect(page.getByRole('alert')).toContainText('Temporary outage')
  fail = false
  await page.getByRole('button', { name: 'Retry', exact: true }).click()
  await expect(page.getByText('No upcoming visits.', { exact: false })).toBeVisible()
  await expect(page.getByRole('alert')).toHaveCount(0)
})

test('a taken slot shows the conflict and clears the old selection', async ({ page }) => {
  await mockApi(page, { overrides: {
    'POST /api/appointments': ({ json }) => json({ detail: 'This slot was just reserved. Choose another time.' }, 409),
  } })
  await page.goto('/book')
  await page.getByLabel('Appointment date').fill('2030-01-07')
  await page.getByRole('radio', { name: '09:00' }).check()
  await page.getByRole('button', { name: 'Confirm booking' }).click()
  await expect(page.getByRole('alert')).toContainText('just reserved')
  await expect(page.getByRole('button', { name: 'Confirm booking' })).toBeDisabled()
  await expect(page.getByRole('radio', { name: '09:00' })).not.toBeChecked()
})

test('admin status changes require confirmation and stale requests show a conflict', async ({ page }) => {
  let stale = true
  await mockApi(page, { user: admin, appointments: [visit], overrides: {
    'PATCH /api/admin/appointments/1/status': ({ request, json, state }) => {
      expect(request.postDataJSON()).toEqual({ status: 'Confirmed', expectedStatus: 'Pending' })
      if (stale) return json({ detail: 'This appointment changed. Refresh before choosing an action.' }, 409)
      state.appointments[0].status = 'Confirmed'
      return json(state.appointments[0])
    },
  } })
  await page.goto('/admin/appointments')
  await page.getByRole('button', { name: 'Confirm', exact: true }).click()
  await page.getByRole('button', { name: 'Confirm change' }).click()
  await expect(page.getByRole('dialog').getByRole('alert')).toContainText('appointment changed')
  await page.getByRole('button', { name: 'Back', exact: true }).click()
  stale = false
  await page.getByRole('button', { name: 'Confirm', exact: true }).click()
  await page.getByRole('button', { name: 'Confirm change' }).click()
  await expect(page.getByRole('button', { name: 'Complete', exact: true })).toBeVisible()
  await fitsViewport(page)
})

test('weekly hours show conflicts without claiming success', async ({ page }) => {
  await mockApi(page, { user: admin, overrides: {
    'PUT /api/admin/schedules/2': ({ json }) => json({ detail: 'These hours would invalidate an active appointment.' }, 409),
  } })
  await page.goto('/admin/schedule')
  await page.getByLabel('Open', { exact: true }).uncheck()
  await page.getByRole('button', { name: 'Save hours' }).click()
  await expect(page.getByRole('alert')).toContainText('invalidate an active appointment')
  await expect(page.getByText('Hours saved.', { exact: true })).toHaveCount(0)
  await fitsViewport(page)
})

test('long patient content fits dashboard profile and history', async ({ page }) => {
  await mockApi(page, {
    user: { ...patient, fullName: 'N'.repeat(100) },
    appointments: [{ ...visit, reasonForVisit: 'R'.repeat(200), status: 'Cancelled', canCancel: false }],
  })
  for (const path of ['/dashboard', '/account', '/appointments/history']) {
    await page.goto(path)
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible()
    if (path.endsWith('history')) await expect(page.getByRole('article')).toBeVisible()
    await fitsViewport(page)
  }
})
