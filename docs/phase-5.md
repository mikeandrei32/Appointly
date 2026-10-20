# Phase 5: Patient dashboard and history

Phase 4 was verified before changes: all 59 backend integration cases, two frontend tests, frontend lint, and the frontend production build passed. This implementation covers Phase 5 only; Phase 6 requires separate approval.

## Patient experience

- Patients land on `/dashboard` after signing in. Administrators still land on `/admin`.
- The dashboard shows upcoming/ongoing and history counts, the nearest or ongoing visit, and links to booking, upcoming visits, history, and profile.
- `/appointments` shows upcoming and ongoing visits, soonest first. Future Pending/Confirmed visits retain the cancellation confirmation flow.
- `/appointments/history` shows past visits and all Completed/Cancelled appointments, newest appointment time first. Cancellation time and reason for visit are retained.
- `/account` remains the profile page.
- Navigation marks the active route visually and with React Router's `aria-current`. Upcoming and History are separate links.
- Dashboard and lists include loading, empty, error, retry, and refresh states. Cancelling reloads the server's lists and moves the visit into history.
- Times are displayed in Asia/Manila. Data is refreshed on page entry and through Refresh; it is not live polling.

## Classification rules

The server captures one current UTC timestamp per overview request.

An appointment is upcoming/ongoing if its status is Pending or Confirmed and its end time is later than that timestamp. At the exact end time it belongs to history. All other appointments belong to history, including future Completed or Cancelled appointments.

The current status is never rewritten by viewing history. An old Pending visit remains Pending until the clinic changes its status. A visit currently in progress remains visible but cannot be cancelled by the patient. Only a future Pending/Confirmed visit has `canCancel: true`; the cancellation endpoint independently enforces this rule again.

Lists use appointment ID as a stable secondary sort when times tie. Both dashboard counts and the displayed visit derive from the same response.

## API and C# concepts

`GET /api/appointments/my/overview` returns:

```json
{
  "upcoming": [],
  "history": []
}
```

Each item uses the existing `AppointmentResponse` DTO, including its ID, UTC appointment time, duration, status, reason, created/cancelled timestamps, and `canCancel`.

- **Claims and authorization:** `AppointmentsController` requires the Patient role. It obtains the patient ID from the authenticated cookie's name identifier claim; the client does not choose whose history to read.
- **Dependency injection:** the controller uses the existing injected `BookingService`. That service receives the database context and `TimeProvider`. No additional service registration is needed.
- **EF Core and LINQ:** `Where` restricts records to the signed-in patient in the database query. `AsNoTracking` avoids change tracking for this read. After loading, straightforward C# lists partition records using status and end time, then LINQ sorts each list.
- **Records and collections:** `PatientOverviewResponse` is a response record with two `IReadOnlyList<AppointmentResponse>` properties. Database entities are not exposed.
- **Method overloading:** `ToResponse(item, now)` reuses one timestamp for all overview cancellation flags; the existing `ToResponse(item)` overload preserves other endpoint behavior.
- **Async and cancellation:** EF's `ToListAsync` receives the request cancellation token. The React hook aborts requests on unmount.
- **Deterministic time:** tests inject a `TimeProvider` and advance it to before, during, and exactly at the end of a visit without waiting.

The endpoint preserves the controller's no-store response policy. Anonymous requests return 401 and admin-only accounts return 403. Existing booking, cancellation, and `/api/appointments/my` endpoints remain available.

This portfolio implementation loads the patient's full appointment collection, as the existing list did. Large histories would benefit from server pagination in a future approved change.

## Complete file inventory

Paths are relative to the repository root; full source is in these files.

| File | Change |
| --- | --- |
| `backend/Appointly.Api/Controllers/AppointmentsController.cs` | New patient overview endpoint |
| `backend/Appointly.Api/DTOs/AppointmentDtos.cs` | Overview response record |
| `backend/Appointly.Api/Services/BookingService.cs` | Ownership-scoped overview, sorting, consistent time snapshot |
| `backend/Appointly.Tests/BookingTests.cs` | Ten new integration cases and overview access checks |
| `frontend/src/components/PatientAppointmentCard.jsx` | New shared visit card |
| `frontend/src/pages/PatientDashboardPage.jsx` | New patient dashboard |
| `frontend/src/pages/MyAppointmentsPage.jsx` | Upcoming/history views, refresh and cancellation feedback |
| `frontend/src/pages/LoginPage.jsx` | Patient dashboard destination |
| `frontend/src/pages/FoundationPage.jsx` | Phase 5 label, copy, and patient destination |
| `frontend/src/layouts/AccountLayout.jsx` | Dashboard/history links and active navigation |
| `frontend/src/App.jsx` | Patient-protected dashboard and history routes |
| `README.md` | Current phase and verification results |
| `docs/phase-5.md` | This guide |

No migrations, dependencies, credentials, or database files are added.

## Verification

From the repository root:

```powershell
dotnet test --artifacts-path "$env:TEMP/appointly-phase5-checks"
npm --prefix frontend run lint
npm --prefix frontend test
npm --prefix frontend run build
git diff --check
```

Results: 69 backend cases and two frontend timezone tests pass; frontend lint/build and whitespace checks pass. The backend test command builds both the API and test projects. Isolated artifacts avoid overwriting an executable held by an existing API process.

New tests cover future/ongoing/end-time/past classification, terminal future statuses, cancellation eligibility, preservation of recorded status, empty accounts, ordering, two-patient isolation, and cancellation moving a record into history. Existing role tests now check the overview route too.

Tests use temporary SQLite databases and in-process HTTP hosting. Development servers were not started or stopped. Interactive browser behavior remains a manual check.

## Run and manual testing

If Phase 4 is already set up, no installation or migration is required.

1. In the API terminal, press Ctrl+C yourself to stop the old version. From the repository root run:

   ```powershell
   dotnet run --project backend/Appointly.Api --launch-profile http
   ```

2. In a second terminal, start the frontend if it is not already running:

   ```powershell
   npm --prefix frontend run dev
   ```

3. Open `http://localhost:5173`. Register/sign in with a fictional patient. Confirm login opens `/dashboard`, shows zero counts for a new account, and provides a booking link.
4. Book two future appointments on different dates. Upcoming should show the earlier one first; the dashboard should show that visit and a count of two.
5. Cancel one through Upcoming. Confirm the count decreases and History shows the cancelled record with its reason and cancellation time. Refresh or navigate back to the dashboard to verify its counts.
6. In a separate private browser window, sign in as admin. Confirm the remaining visit; refresh the patient page and verify Confirmed. Complete it; refresh again and verify it moved to History with no cancellation button.
7. Sign in as a different patient and verify none of the first patient's appointments appear.
8. Open History directly, reload, and navigate between Dashboard, Upcoming, History, Booking, and My profile. Check the correct navigation link is marked active. Sign out and verify `/dashboard` and `/appointments/history` require login.
9. Stop the API yourself, then use Refresh on a patient page. Verify an error and Retry appear. Restart the API and retry. Use browser network throttling to inspect loading feedback.
10. At a narrow browser width, verify navigation wraps, long visit reasons wrap, and buttons remain reachable. Time-boundary behavior is covered by the clock-controlled tests; an actual visit should move to history when refreshed at or after its end.

The previous administrator setup script remains available if needed: `.\scripts\Start-DevelopmentAdmin.ps1` starts the API after prompting for development administrator credentials.
