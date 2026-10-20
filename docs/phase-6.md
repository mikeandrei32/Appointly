# Phase 6: Testing and polish

Phase 5 was inspected and verified before changes: 69 backend tests, two frontend tests, lint, and the production build passed. Phase 7 portfolio documentation is not implemented here.

## Changes

- Added Playwright tests using Chromium at 1280×900 and 320×740.
- Tests run against a fresh production build and an isolated preview at `127.0.0.1:4178`. Playwright starts and stops that preview. It refuses to reuse an existing server.
- API responses are intercepted in browser tests, so no application database or development API is accessed.
- Added a fail-fast PowerShell check script for backend tests, lint, unit tests, browser tests/build, and whitespace validation.
- Fixed dashboard and profile overflow with long names, and blocked-date overflow with long reasons. Text wraps rather than being clipped.
- Fixed focus loss when dismissing patient/admin confirmation dialogs: ordinary dismissal keeps the trigger button mounted. Successful changes and dismissals after errors still reload current records.
- Updated the development phase badge and testing instructions.

## Coverage

| Requirement | Verification |
| --- | --- |
| Role restrictions | Existing real API tests for anonymous/patient/admin access; browser route rejection tests |
| Appointment conflicts | Existing SQLite competing bookings and booking-versus-availability races; browser 409 recovery clears selection |
| Invalid status transitions | Existing all-16-transition matrix and stale-status checks; browser confirmation/error feedback |
| Cancellation rules | Existing ownership, past/started, terminal-status and reservation-release cases; browser cancel/history flow |
| Timezone behavior | Existing Manila midnight and UTC conversion tests; browser set to Los Angeles while visits display 09:00 Manila |
| Responsive layouts | Real Chromium viewport checks for booking, profile, dashboard, history, admin tables, and blocked dates |
| Registration and authentication UI | Password mismatch, success redirect, login, logout, protected route checks |
| Failure feedback | Loading, error, retry, empty dashboard, schedule conflicts, stale status requests |
| Keyboard focus | Escape closes the cancellation dialog and returns focus to its trigger |

The browser suite has ten scenarios run in both viewport projects: 20 cases. The browser clock is fixed to January 2030 so date minimums do not depend on the day the tests run.

The mock API is a UI fixture, not a second implementation of the server. It supplies only the state needed by each scenario, rejects unexpected requests, and does not establish that real authentication or persistence works. The 69 .NET integration cases remain the evidence for server behavior.

## Bugs found by tests

The first layout run detected dashboard overflow with a 100-character name at both widths. Extended tests also exposed profile-grid and blocked-date overflow. Explicit grid tracks and shrinkable flex children now allow the existing wrapping rules to work.

A keyboard assertion found that dismissing a dialog called reload, which removed the focused trigger from the DOM. Dismissal now reloads only after an error; successful mutations explicitly reload. Normal dismissal allows the browser's native dialog to restore focus.

## C# and ASP.NET Core concepts

No backend production changes were needed in this phase.

- **WebApplicationFactory:** existing tests host the actual ASP.NET Core pipeline in-process, exercising routing, authorization, validation, and antiforgery through HTTP.
- **Real SQLite:** temporary databases apply migrations and test real transactions and uniqueness constraints; no in-memory substitute hides relational behavior.
- **Dependency injection:** tests substitute `TimeProvider` for deterministic date boundaries while using the normal application services.
- **xUnit facts and theories:** facts cover workflows; parameterized theories cover invalid times and status combinations.
- **Authorization versus UI routing:** the frontend redirects users to appropriate pages; `[Authorize]` enforces access on the server independently.

Frontend browser tests complement those checks by clicking actual controls and measuring page width. Tables intentionally scroll inside their own region; the page itself must fit the viewport.

## Files changed

Paths are relative to the repository root.

| File | Responsibility |
| --- | --- |
| `.gitignore` | Ignore browser reports, screenshots, traces, and results |
| `frontend/package.json` | Playwright development dependency and browser-test command |
| `frontend/package-lock.json` | Locked test dependencies |
| `frontend/playwright.config.js` | Viewports, timezone, temporary preview, failure artifacts |
| `frontend/tests/app.spec.js` | Ten browser scenarios and explicit API fixtures |
| `scripts/Test-Appointly.ps1` | Full fail-fast verification sequence |
| `frontend/src/pages/PatientDashboardPage.jsx` | Long-name flex wrapping |
| `frontend/src/pages/AccountPage.jsx` | Profile heading and grid wrapping |
| `frontend/src/pages/AdminSchedulePage.jsx` | Long blocked-date reason wrapping |
| `frontend/src/pages/MyAppointmentsPage.jsx` | Preserve focus on ordinary dismissal |
| `frontend/src/pages/AdminAppointmentsPage.jsx` | Preserve focus on ordinary dismissal |
| `frontend/src/pages/FoundationPage.jsx` | Phase 6 badge |
| `README.md` | Current status and check commands |
| `docs/phase-6.md` | This guide |

## Install and run checks

From the repository root, install the new test tools once:

```powershell
npm --prefix frontend ci
Push-Location frontend
npx playwright install chromium
Pop-Location
```

Run everything:

```powershell
.\scripts\Test-Appointly.ps1
```

Or run only browser tests:

```powershell
npm --prefix frontend run test:browser
```

The browser command builds first. Failure screenshots and traces are saved under `frontend/test-results`, ignored by Git. Playwright prints a command for opening a failed trace. Do not run tests against an existing server on port 4178; free that port or adjust the test configuration.

Results: 69 backend tests, two timezone unit tests, and 20 browser cases pass. Lint, backend compilation, production frontend build, and whitespace validation pass. No new database migration is required.

## Manual verification with your real API

1. Start the API yourself if needed:

   ```powershell
   dotnet run --project backend/Appointly.Api --launch-profile http
   ```

2. Start the frontend yourself if needed:

   ```powershell
   npm --prefix frontend run dev
   ```

3. Open `http://localhost:5173`, register fictional patient data, and book a future weekday slot. Verify it appears on the dashboard.
4. In a private window, use another patient and try the same slot. Verify it is unavailable or returns a conflict. Cancel the first booking and verify the slot becomes available.
5. As administrator, confirm a booking, complete it, and verify no further transition buttons appear. Try closing/blocking a date with another active booking; verify the conflict.
6. Use the keyboard to open and dismiss a cancellation dialog with Escape. Confirm focus returns to the button.
7. At 320-pixel width, inspect long names/reasons, date/time inputs, wrapped navigation, and horizontal scrolling within admin tables.
8. Repeat essential flows in your preferred browser. Automated coverage is Chromium; Firefox, WebKit, screen-reader behavior, and physical devices are not certified by these tests.

The real browser-to-API combination still merits this manual smoke check: browser tests isolate UI behavior, while .NET tests verify the real server separately.
