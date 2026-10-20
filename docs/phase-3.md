# Phase 3 — Appointment booking

## Scope

Patients can view available slots, book a 30-minute appointment, list their own appointments, and cancel a future Pending or Confirmed appointment. New appointments start Pending. Admin screens, admin status transitions, schedule editing, dashboards, and dedicated history pages remain in their later phases.

BlockedDate storage and enforcement are included because booking must reject blocked dates. Creating and managing these dates through the UI is Phase 4. Integration tests populate blocked dates directly to verify the rule.

## API

All routes require the Patient role. Mutation routes also require the existing CSRF header and cookie.

| Method | Route | Behavior |
| --- | --- | --- |
| GET | /api/availability?date=YYYY-MM-DD | Returns date, Asia/Manila timezone, duration, and slots with UTC starts and local labels |
| GET | /api/appointments/my | Returns the signed-in patient's appointments |
| GET | /api/appointments/{id} | Returns an owned appointment; missing or another patient's ID returns 404 |
| POST | /api/appointments | Creates a Pending booking; returns 201 with a Location header |
| PATCH | /api/appointments/{id}/cancel | Cancels an eligible booking and releases its reservation; returns 200 |

Booking request example:

```json
{
  "appointmentDateTimeUtc": "2030-01-07T01:00:00Z",
  "reasonForVisit": "Routine visit"
}
```

This example represents 09:00 in Manila; use an actual future available slot when testing. An explicit timezone is required. Offset timestamps are normalized to UTC. The request cannot set patient ID, status, or duration. Unknown properties are rejected. The optional reason is limited to 200 characters; do not include sensitive information.

Validation failures return 400. A taken slot or ineligible cancellation returns 409. Anonymous requests return 401 and non-patient accounts return 403. Only the current patient's identity from the authentication cookie is used.

## Scheduling and race protection

- Stored appointment timestamps use UTC. Scheduling uses Asia/Manila regardless of the browser or server's local timezone.
- The slot grid is on the hour or half hour, with zero seconds/fractions. The entire 30-minute visit must fit within clinic hours.
- Default hours remain Monday-Friday 09:00-17:00. The final available start is 16:30. The service reads the schedule table; the hours are not hardcoded in React.
- Slots at or before the current time, closed weekdays, and blocked dates cannot be booked.
- A SlotReservation primary key is its UTC start. SQLite rejects two reservations for the same slot even if a caller bypasses the preliminary availability check.
- Booking opens an immediate SQLite write transaction before checking the schedule and reservations. The appointment and reservation are saved together. A failure rolls both back.
- Concurrent booking requests serialize at SQLite's write lock. After the winner commits, the next request sees the reservation and returns 409. The unique key remains the database-level backstop.
- Cancellation changes status, records audit timestamps, and deletes the reservation in the same transaction. It never deletes the historical appointment.
- Repeat cancellation, completed records, and appointments at or before their start time cannot be cancelled. Cancelling another patient's appointment returns 404.
- A unique reservation conflict or SQLite write contention produces a safe 409 response. The booking page refreshes availability after a failed submission.

When Phase 4 implements schedule changes, those writes must also acquire an immediate SQLite transaction before checking existing bookings. The policy remains to reject changes that invalidate future active appointments; this phase does not implement schedule mutation endpoints.

## C# and ASP.NET Core concepts

- **Entities:** Appointment, SlotReservation, and BlockedDate are classes mapped by EF Core to SQLite tables. AppointmentStatus is an enum, stored as readable status strings.
- **Relationships:** an appointment belongs to a patient and has at most one reservation. Patient foreign keys prevent orphaned bookings. Reservation uniqueness enforces ownership of each slot.
- **Encapsulation:** BookingService holds booking rules so controllers remain focused on HTTP. Private methods share schedule and status checks.
- **Dependency injection:** ASP.NET Core creates BookingService per request and supplies its AppointlyDbContext and TimeProvider. Injecting a clock allows deterministic tests of time boundaries.
- **Async/await:** database queries and saves are awaited. SQLite's immediate transaction acquisition is synchronous because that provider exposes its immediate-lock option there.
- **DTOs:** request validation and response records keep EF entities and patient identity fields out of the public API.
- **Exception filter:** BookingException carries an expected HTTP status; BookingExceptionFilter converts it into a safe ProblemDetails response. Unexpected exceptions still use the existing global handler.
- **React/API communication:** appointments.js uses the existing Fetch/CSRF helper. The UI displays backend slots and CanCancel flags, while the backend independently checks every request.

## Files added or modified

Backend:

- Models/Appointment.cs, Models/SlotReservation.cs, Models/BlockedDate.cs
- Data/AppointlyDbContext.cs
- Data/Migrations/20261020091736_AddAppointmentBooking.cs and its Designer file
- Data/Migrations/AppointlyDbContextModelSnapshot.cs
- DTOs/AppointmentDtos.cs
- Services/BookingService.cs, Services/BookingException.cs, Services/ClinicTime.cs
- Configuration/BookingExceptionFilter.cs
- Controllers/AppointmentsController.cs, Controllers/AvailabilityController.cs
- Program.cs
- Appointly.Tests/AuthFactory.cs, Appointly.Tests/BookingTests.cs

Frontend:

- src/services/appointments.js
- src/utils/clinicTime.js, src/utils/clinicTime.test.js
- src/components/TimeSlotPicker.jsx, StatusBadge.jsx, ConfirmDialog.jsx
- src/pages/BookAppointmentPage.jsx, MyAppointmentsPage.jsx
- src/App.jsx, src/layouts/AccountLayout.jsx
- src/pages/AccountPage.jsx, FoundationPage.jsx
- package.json (adds the native Node test command; no new dependencies)

Documentation: README.md and this guide.

## Checks and results

Before changes, the ten Phase 2 SQLite integration cases and frontend lint/build passed.

After changes:

```powershell
dotnet test
dotnet ef migrations has-pending-model-changes --project backend/Appointly.Api
npm --prefix frontend run lint
npm --prefix frontend test
npm --prefix frontend run build
```

The backend suite contains 30 passing cases: authentication regression coverage plus booking validation, ownership, CSRF, blocked/closed dates, availability boundaries, duplicate reservations, simultaneous requests, cancellation/rebooking, and status restrictions. Tests apply real migrations to temporary SQLite files, never EF InMemory. The direct constraint test bypasses the service and proves SQLite rejects a duplicate and rolls back the attempted appointment insert.

The two Node tests verify Manila midnight and UTC-to-local formatting. Frontend lint and build pass. Browser interaction and responsive visual inspection were not performed in this phase; no development servers were started.

## Run and manually verify

1. Stop your existing backend with Ctrl+C before rebuilding or migrating, to avoid Windows executable locks.
2. From the project root:

   ```powershell
   dotnet tool restore
   dotnet ef database update --project backend/Appointly.Api
   dotnet run --project backend/Appointly.Api --launch-profile http
   ```

3. In your separate frontend terminal:

   ```powershell
   npm --prefix frontend run dev
   ```

4. Open http://localhost:5173 and sign in as a patient. Choose **Book appointment**. Select a future weekday and one of the returned times. Confirm that the summary uses Manila time.
5. Submit. **My appointments** should show the saved booking as Pending. Refresh the page to verify persistence.
6. Select a weekend. It should show no slots. For today, already-started slots should not appear.
7. To test a conflict, use two different patient accounts in separate browser/private sessions. Have both select the same available slot before either submits. Submit the first, then the second. Only the first should succeed; the second should show a conflict and refreshed availability.
8. On **My appointments**, choose **Cancel appointment**. First select **Keep appointment** to verify nothing changes. Then cancel and confirm. The record remains Cancelled and no longer offers cancellation.
9. In the other session, refresh that date and book the released time.
10. Sign in as an administrator. Patient booking links should be absent and direct patient API calls should return 403. Admin appointment management remains Phase 4.

The list is a basic booking-management page for Phase 3. Dedicated upcoming/history views and dashboard statistics are not included. No emails or medical records are collected. This phase ends here; Phase 4 requires approval.
