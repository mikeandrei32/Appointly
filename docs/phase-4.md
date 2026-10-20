# Phase 4: Admin management

Only Phase 4 is implemented here. Phase 3 was verified first: 30 backend integration tests, two frontend timezone tests, frontend lint, and the production frontend build passed.

## Implemented behavior

- `/admin`: database-backed totals by status, today's appointment count in Asia/Manila, and the eight most recently created appointments. Today includes every status.
- `/admin/appointments`: patient name/email, clinic-date, and status filters; pagination; confirm, complete, and cancel actions with confirmation dialogs.
- `/admin/schedule`: edit each weekday's hours, close weekdays, block specific dates, and remove blocks.
- Loading, empty, error, retry, and success feedback. The appointment table scrolls horizontally on narrow screens.
- All administration endpoints require the Admin role. Mutations retain cookie authentication and CSRF validation.

## Rules

| Current status | Allowed next status |
| --- | --- |
| Pending | Confirmed, Cancelled |
| Confirmed | Completed, Cancelled |
| Completed or Cancelled | None |

Status requests include `expectedStatus`. A stale status produces 409 so a second administrator cannot silently overwrite a status change. Completing or cancelling deletes the slot reservation and updates the appointment audit timestamp; cancellation also records its own timestamp. Completion is an explicit administrator action without a time restriction.

Weekly hours use `HH:mm`, 30-minute boundaries, and closing after opening on the same day. Closed rows retain valid hours for reopening. Changes that invalidate any Pending or Confirmed future or ongoing visit are rejected with 409. Resolve the appointment first; schedule edits never silently cancel bookings.

New blocked dates must be today or later in Manila. Duplicate blocks and blocks conflicting with active future/ongoing visits return 409. Unblocking restores the usual weekly hours. Existing historical blocks remain visible and removable.

Booking, cancellation, status changes, and availability edits share an immediate SQLite transaction. The write lock is obtained before reading availability or conflicting appointments. A concurrent booking versus a closure/block cannot both succeed in creating an inconsistent schedule.

## C# and ASP.NET Core concepts

- **Dependency injection:** `Program.cs` registers scoped `AdminService` and `ScheduleWriter`. Controllers receive services through constructor parameters. Scoped services share the request's database context.
- **DTOs and records:** request classes describe accepted input and use validation attributes. Response records expose only the fields needed by the UI, instead of serializing EF entities and navigation properties.
- **Authorization and antiforgery:** `[Authorize(Roles = AppRoles.Admin)]` enforces roles on the server. The existing global antiforgery filter protects unsafe HTTP methods; hiding a React route is not the access-control boundary.
- **LINQ and EF Core:** filtering, counting, ordering, and pagination become database queries. `AsNoTracking()` avoids tracking entities that will not be edited. `Include()` loads the patient or reservation when needed.
- **Async and cancellation:** asynchronous EF calls avoid blocking while awaiting database operations. Request cancellation tokens flow through controller and service calls.
- **Transactions:** `ScheduleWriter.WriteAsync<T>` accepts an asynchronous operation returning `T`. It commits the operation atomically; exceptions roll it back. The immediate SQLite lock protects the read-check-write sequence.
- **State transitions:** a C# switch expression explicitly defines legal appointment transitions. Existing `BookingExceptionFilter` converts validation/conflict exceptions into safe HTTP ProblemDetails.
- **TimeProvider:** injectable time makes Manila date boundaries and ongoing-visit conflicts deterministic in integration tests. Stored appointment timestamps remain UTC.

## API

All routes below are under `/api/admin`.

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/dashboard` | Counts and recent appointments |
| GET | `/appointments` | Optional date, patient, status, page, pageSize filters |
| PATCH | `/appointments/{id}/status` | Change status with status and expectedStatus |
| GET | `/schedules` | Seven weekday schedules |
| PUT | `/schedules/{id}` | Save isOpen, openingTime, closingTime |
| GET | `/blocked-dates` | List blocked dates |
| POST | `/blocked-dates` | Add date and optional reason |
| DELETE | `/blocked-dates/{id}` | Remove block |

Default page size is 20; maximum is 100. Invalid inputs return 400, missing records 404, and conflicts 409. Anonymous requests return 401 and patient requests 403. The Phase 2 `/api/admin/access` endpoint remains available.

## Modified files

Paths below are relative to the repository root.

Backend:

- `backend/Appointly.Api/Controllers/AdminController.cs` (new)
- `backend/Appointly.Api/DTOs/AdminDtos.cs` (new)
- `backend/Appointly.Api/Services/AdminService.cs` (new)
- `backend/Appointly.Api/Services/ScheduleWriter.cs` (new)
- `backend/Appointly.Api/Services/BookingService.cs`
- `backend/Appointly.Api/Program.cs`
- `backend/Appointly.Tests/AdminTests.cs` (new)

Shared frontend code:

- `frontend/src/services/admin.js` (new)
- `frontend/src/hooks/useApiData.js` (new)
- `frontend/src/components/AdminAppointmentTable.jsx` (new)
- `frontend/src/components/ScheduleEditor.jsx` (new)
- `frontend/src/components/ConfirmDialog.jsx`

Pages and navigation:

- `frontend/src/pages/AdminDashboardPage.jsx` (new)
- `frontend/src/pages/AdminAppointmentsPage.jsx` (new)
- `frontend/src/pages/AdminSchedulePage.jsx` (new)
- `frontend/src/pages/AdminAccessPage.jsx` (removed; replaced by dashboard)
- `frontend/src/pages/AccountPage.jsx`
- `frontend/src/pages/FoundationPage.jsx`
- `frontend/src/layouts/AccountLayout.jsx`
- `frontend/src/App.jsx`

Documentation: `README.md` and `docs/phase-4.md` (new).

## Automated verification

From the repository root:

```powershell
dotnet test --artifacts-path "$env:TEMP/appointly-phase4-checks"
npm --prefix frontend run lint
npm --prefix frontend test
npm --prefix frontend run build
```

Results: 59 backend integration cases and two frontend tests pass; frontend lint/build pass. The backend test command also builds the API and test projects. Tests use isolated SQLite databases and in-process HTTP hosting, without starting a development server or modifying the local application database.

The 29 new cases cover real dashboard/filter results, Manila boundaries, all 16 status transitions, stale status requests, slot release, invalid hours, conflicts with future/ongoing appointments, block/unblock behavior, role/CSRF checks, and simultaneous booking versus blocking or closure. Browser interaction has not been automated.

## Run and manually test

No new migration or dependency installation is required if Phase 3 is already set up. The running API must be restarted by you to load the new backend code.

1. In your API terminal, press Ctrl+C, then run from the repository root:

   ```powershell
   dotnet run --project backend/Appointly.Api --launch-profile http
   ```

   If you have not created an administrator, use `.\scripts\Start-DevelopmentAdmin.ps1` instead. It prompts for credentials; see the Phase 2 guide.

2. In a second terminal, start the frontend yourself:

   ```powershell
   npm --prefix frontend run dev
   ```

   If it is already running, use that instance and refresh the browser.

3. Open `http://localhost:5173`. Confirm Phase 4 and the green health status. Log in as a patient and book a future weekday appointment using fictional data.
4. Log in as administrator in a separate browser/private window. Open `/admin`: verify the counts and recent booking match the patient appointment.
5. Open Appointments. Filter by the patient name/email, date, and Pending status. Confirm the visit; verify it becomes Confirmed. Complete it; verify Completed and the absence of further actions. Check the updated dashboard totals.
6. Make another patient booking. Cancel it through the admin table; confirm the patient sees Cancelled and the future slot becomes available.
7. With an active future booking, try closing its weekday or shortening hours to exclude it. Expect a conflict and unchanged hours. Try blocking its date; expect a conflict.
8. On a weekday without conflicting bookings, save 10:00-12:00 hours. Check that patient availability respects those hours. Block a free future date; verify no slots. Unblock it; verify the weekly slots return.
9. Open the same appointment in two administrator tabs. Change its status in one, then submit an action from the stale tab. Expect the refresh/conflict message. Close the dialog to reload.
10. Sign in as a patient and navigate to `/admin`; verify access is denied by routing. Sign out and verify protected routes redirect to login. Check forms and table scrolling at a narrow browser width.

Phase 5 remains unimplemented and requires separate approval.
