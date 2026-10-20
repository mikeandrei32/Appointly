import { test } from 'node:test'
import assert from 'node:assert/strict'
import { clinicDate, formatAppointment } from './clinicTime.js'

test('clinic date switches at Manila midnight, not UTC midnight', () => {
  assert.equal(clinicDate(new Date('2030-01-07T15:59:59Z')), '2030-01-07')
  assert.equal(clinicDate(new Date('2030-01-07T16:00:00Z')), '2030-01-08')
})

test('appointment display converts UTC to Manila local time', () => {
  assert.match(formatAppointment('2030-01-07T01:00:00Z'), /9:00/)
})
