# Phase 7: Portfolio documentation

This completes the seventh and final phase of the agreed plan. No additional application features or deployment are included.

## Delivered

- Reworked the root README into a portfolio entry with features, stack, architecture, folder structure, database setup, configuration, installation, run commands, migrations, admin setup, API documentation, tests, security notes, limitations, and proposed future improvements.
- Added a concise portfolio description based on implemented behavior.
- Captured and visually reviewed three screenshots of the actual React application: patient dashboard, mobile booking, and admin appointments.
- Added a separate, reproducible Playwright capture command. Regression tests and documentation captures remain separate.

## Screenshot provenance

The PNGs in `docs/screenshots/` were captured locally in Chromium from a production frontend build. Their names, emails, appointments, and availability come from fictional API fixtures in `frontend/screenshots/portfolio.spec.js`. The browser clock is fixed at January 6, 2030; displayed appointments are in January 2030. The browser timezone is Los Angeles, while appointment display remains Asia/Manila.

The patient dashboard shows one visit belonging to Alex Reyes. The administrator table shows Alex Reyes and Sam Cruz. The mobile booking image demonstrates a selected slot without submitting an appointment. All email addresses use `example.test`.

These screenshots show implemented UI, not a live deployment or a real database session. The capture fixture does not write application data.

| Image | Capture dimensions |
| --- | --- |
| `screenshots/patient-dashboard.png` | 1440 × 1000 viewport, full page |
| `screenshots/admin-appointments.png` | 1440 × 900 viewport, full page |
| `screenshots/mobile-booking.png` | 390 × 844 viewport, full page |

The full mobile image is taller than the viewport so the complete form and summary are visible.

## Regenerate

From the repository root, after dependency installation:

```powershell
Push-Location frontend
npx playwright install chromium
Pop-Location
npm --prefix frontend run screenshots
```

The command builds first, uses the shared isolated preview configuration on port 4178, and replaces the three PNGs. Keep that port free. It does not start the development API or the Vite development server. Review regenerated images before committing.

## C# and ASP.NET Core concepts documented

No C# source changes were necessary. The README explains:

- Constructor dependency injection and scoped database contexts.
- Request validation, response DTO records, and structured ProblemDetails.
- Claim-based patient identity and server-side role authorization.
- Asynchronous EF queries and request cancellation.
- Immediate transactions and unique slot reservations.
- Injectable time for deterministic UTC/Manila boundary tests.

These explanations were checked against the current source, including `Program.cs`, `BookingService`, `ScheduleWriter`, the EF mappings, and the development admin seeder.

## Verification

Before changes, `scripts/Test-Appointly.ps1` passed all Phase 6 checks:

- 69 actual API/SQLite integration cases.
- Two frontend timezone unit tests.
- 20 Chromium browser cases across desktop and mobile viewports.
- Frontend lint, backend compilation, frontend production build, and whitespace checks.

The new screenshot command separately builds the frontend and passes its capture scenario. All three resulting images were visually inspected. Local documentation links and image targets were checked. Browser test fixtures and screenshot fixtures are explicitly distinguished from real backend integration evidence.

## Modified and added files

| File | Responsibility |
| --- | --- |
| `README.md` | Complete portfolio documentation |
| `frontend/package.json` | Explicit screenshots command |
| `frontend/playwright.screenshots.config.js` | Separate capture suite reusing the preview configuration |
| `frontend/screenshots/portfolio.spec.js` | Fictional fixtures and deterministic page captures |
| `docs/screenshots/patient-dashboard.png` | Patient overview screenshot |
| `docs/screenshots/mobile-booking.png` | Responsive booking screenshot |
| `docs/screenshots/admin-appointments.png` | Admin management screenshot |
| `docs/phase-7.md` | Phase handoff, provenance, and verification |

No migrations, dependency additions, production code changes, or credentials are included.

## Manual review

1. Open the root README on GitHub. Confirm all three screenshots render and phase-guide links work.
2. Compare the documented environment variables and run commands with your local configuration.
3. Use the README's installation steps on a separate fresh checkout if you want a clean-machine onboarding check. An independent clean-machine installation was not performed in this phase.
4. Start the API and frontend yourself using the README commands. Verify login, patient booking/cancellation, and admin schedule conflicts with fictional data.
5. Optionally regenerate screenshots and inspect the three PNGs for clipping, stale content, or failed loading states before committing.

The application remains local-only. Future improvements listed in the README are proposals requiring separate approval.
