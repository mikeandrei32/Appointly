# Appointly

A clinic appointment booking portfolio project using React (JavaScript), ASP.NET Core, Entity Framework Core, and SQLite.

## Current status

Phases 1 and 2 are implemented: frontend foundation, database-backed health endpoint, migrations, development OpenAPI, patient registration/login/logout, profile, Patient/Admin authorization, CSRF protection, and development-only admin setup. Booking, appointment management, and dashboards are not implemented yet. This is not a production medical records system.

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
- AppointlyDbContext inherits DbContext, configures the unique weekday index, and seeds schedule defaults through migrations.
- HealthController receives AppointlyDbContext and ILogger through constructor dependency injection. ASP.NET Core supplies them per request.
- HealthResponse is an immutable response DTO. The API does not expose database entities.
- The health endpoint queries a migrated table, returning 503 with safe ProblemDetails if the schema or database is unavailable.
- React's health.js uses native Fetch. Vite forwards /api requests to the backend, avoiding cross-origin development cookies later.
- FoundationPage renders actual health responses, loading feedback, errors, and retry. Brand is a reusable presentation component.

## Checks

```powershell
dotnet build
npm --prefix frontend run lint
npm --prefix frontend run build
Invoke-RestMethod http://localhost:5080/api/health
Invoke-RestMethod http://localhost:5173/api/health
Invoke-RestMethod http://localhost:5173/openapi/v1.json
```

Verified during Phase 1: migration generation/application, backend build with zero warnings/errors, frontend lint/build, HTTP 200 for the frontend, healthy responses directly and through the proxy, and development OpenAPI containing /api/health. Browser rendering and the connected status were inspected. Phase 2 adds ten SQLite-backed xUnit integration cases. A full mobile/device matrix has not yet been verified.

## Migrations

After intentionally changing the database model, create a new migration and review its generated code:

```powershell
dotnet ef migrations add DescribeYourChange --project backend/Appointly.Api --output-dir Data/Migrations
dotnet ef database update --project backend/Appointly.Api
```

Commit migration source files, not the local database. The application does not silently apply migrations on startup.

## Authentication setup and verification

See [Phase 2 guide](docs/phase-2.md) for API routes, development admin setup, tests, manual verification, and class responsibilities.

## Roadmap

2. Complete: Identity, Patient/Admin roles, CSRF, authentication UI, and development admin configuration.
3. Appointment booking, availability, cancellation, unique slot reservations, and concurrency tests.
4. Administration and schedule conflict handling.
5. Patient dashboard and history.
6. SQLite-backed integration tests and responsive polish.
7. Portfolio documentation and screenshots of completed features.

No deployment or completed booking functionality is claimed. Never enter real patient data into this demonstration.
