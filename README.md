# Appointly

A clinic appointment booking portfolio project using React (JavaScript), ASP.NET Core, Entity Framework Core, and SQLite.

## Current status

Phases 1-6 are implemented: frontend foundation, database-backed health endpoint, migrations, development OpenAPI, patient registration/login/logout, profile, Patient/Admin authorization, CSRF protection, development-only admin setup, appointment availability, booking, patient cancellation, administrator dashboard, appointment management, weekly hours, blocked dates, and a patient dashboard with upcoming visits and appointment history. Phase 6 adds browser regression tests and responsive/accessibility fixes. This is not a production medical records system.

## Prerequisites

- .NET 10 SDK (developed with 10.0.203; global.json allows newer feature bands in .NET 10).
- Node.js 24 and npm.
- Git for version control.

## Setup

Run in the appointly root directory:

```powershell
dotnet tool restore
dotnet restore
dotnet ef database update --project backend/Appointly.Api
npm --prefix frontend ci
```

The migration creates backend/Appointly.Api/appointly.db and seeds seven weekday schedule rows. Monday-Friday are open 09:00-17:00; weekends are closed. Times represent Asia/Manila clinic time. No patient records or administrator credentials are seeded by migrations. Identity tables are added by the Phase 2 migration. An explicitly configured development administrator can be seeded at startup.

## Run locally

Terminal 1, from the project root:

```powershell
dotnet run --project backend/Appointly.Api --launch-profile http
```

Terminal 2, from the project root:

```powershell
npm --prefix frontend run dev
```

Open http://localhost:5173. The page should show "API and database connected". The refresh button retries the health request. Stop each server with Ctrl+C.

- API health: http://localhost:5080/api/health
- Development OpenAPI JSON: http://localhost:5080/openapi/v1.json
- Proxied API health: http://localhost:5173/api/health

OpenAPI is a machine-readable specification; a Swagger UI is not installed. Only development exposes the specification. The Vite proxy is for development; serving the production build requires a host with SPA fallback and an /api reverse proxy.

## Configuration

The API uses appsettings.json, optional gitignored appsettings.Local.json, and environment variables (environment variables override the local file).

| Setting | Default / purpose |
| --- | --- |
| ConnectionStrings__DefaultConnection | Data Source=appointly.db; relative to API content root when launched with dotnet run |
| Cors__AllowedOrigins__0 | Optional explicitly allowed frontend origin; no cross-origin access by default |
| AllowedHosts | localhost;127.0.0.1; configure actual hostnames for deployment |
| API_PROXY_TARGET | http://localhost:5080; optional frontend/.env value for Vite |

Copy frontend/.env.example to frontend/.env only if changing the API target. Never put secrets in VITE_ variables because they are exposed to the browser. Local configuration, database files, dependencies, and build output are ignored by Git.

The default HTTP profile is for local development. For the HTTPS API profile:

```powershell
dotnet dev-certs https --trust
dotnet run --project backend/Appointly.Api --launch-profile https
```

The HTTPS profile also retains HTTP for the local Vite proxy. Production enables HTTPS redirection and HSTS. Identity cookie authentication and CSRF protection are implemented. Production cookies require HTTPS. Prefer one origin for deployment; cross-site cookie hosting is not configured. See docs/phase-2.md for authentication details.

## Architecture and learning guide

- Program.cs registers controllers, OpenAPI, error handling, optional CORS, and the SQLite database context.
- ClinicSchedule is a class representing one weekday's hours. Its properties hold data that EF Core persists.
- AppointlyDbContext inherits IdentityDbContext<ApplicationUser>, configures clinic and booking tables, and seeds schedule defaults through migrations.
- BookingService enforces clinic hours, Manila time, ownership, and cancellation rules. SQLite transactions and a unique slot reservation prevent double booking.
- HealthController receives AppointlyDbContext and ILogger through constructor dependency injection. ASP.NET Core supplies them per request.
- HealthResponse is an immutable response DTO. The API does not expose database entities.
- The health endpoint queries a migrated table, returning 503 with safe ProblemDetails if the schema or database is unavailable.
- React's health.js uses native Fetch. Vite forwards /api requests to the backend, avoiding cross-origin development cookies later.
- FoundationPage renders actual health responses, loading feedback, errors, and retry. Brand is a reusable presentation component.

## Checks

```powershell
dotnet build
dotnet test
npm --prefix frontend run lint
npm --prefix frontend test
npm --prefix frontend run build
Invoke-RestMethod http://localhost:5080/api/health
Invoke-RestMethod http://localhost:5173/api/health
Invoke-RestMethod http://localhost:5173/openapi/v1.json
```

Phase 6 verification: 69 SQLite-backed integration cases, two frontend timezone tests, and 20 Chromium browser cases pass. Frontend lint/build and whitespace checks pass. Browser cases run against a temporary production preview at port 4178 with mocked API responses; backend integration cases exercise real Identity and SQLite. Existing development servers and application data are untouched. No new migration is required.

Install the browser test dependency and Chromium once after pulling:

```powershell
npm --prefix frontend ci
Push-Location frontend
npx playwright install chromium
Pop-Location
```

Run the full check sequence from the repository root:

```powershell
.\scripts\Test-Appointly.ps1
```

The script stops on the first failed check. Browser tests cover desktop and 320-pixel mobile viewports; Firefox, WebKit, and physical devices remain manual checks.

## Migrations

After intentionally changing the database model, create a new migration and review its generated code:

```powershell
dotnet ef migrations add DescribeYourChange --project backend/Appointly.Api --output-dir Data/Migrations
dotnet ef database update --project backend/Appointly.Api
```

Commit migration source files, not the local database. The application does not silently apply migrations on startup.

## Authentication setup and verification

See [Phase 2 guide](docs/phase-2.md) for API routes, development admin setup, tests, manual verification, and class responsibilities.

See [Phase 3 guide](docs/phase-3.md) for booking rules, transaction behavior, changed files, and manual booking/cancellation tests.

See [Phase 4 guide](docs/phase-4.md) for administrator management, conflict rules, changed files, and manual verification.

See [Phase 5 guide](docs/phase-5.md) for the patient dashboard, upcoming/history rules, modified files, and manual verification.

See [Phase 6 guide](docs/phase-6.md) for test coverage, responsive fixes, browser-test setup, and manual verification.

## Roadmap

2. Complete: Identity, Patient/Admin roles, CSRF, authentication UI, and development admin configuration.
3. Complete: appointment booking, availability, cancellation, unique slot reservations, and concurrency tests.
4. Complete: administrator dashboard, appointment status management, weekly hours, blocked dates, and schedule conflict handling.
5. Complete: patient dashboard, upcoming visits, history, navigation, and loading/empty/error feedback.
6. Complete: backend regression verification, browser tests, responsive fixes, and keyboard-focus polish.
7. Portfolio documentation and screenshots of completed features.

The application has not been deployed. Never enter real patient data into this demonstration.
