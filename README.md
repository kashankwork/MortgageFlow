# MortgageFlow

MortgageFlow is a mortgage workflow simulation.

It is an educational project. It does not claim to reproduce internal systems, proprietary workflows, or business rules.

## Current foundation

The current implementation establishes:

- A modular-monolith .NET solution.
- A pure Domain project with guarded entities and value objects.
- A tested loan workflow state machine.
- A Vite React + TypeScript frontend scaffold.
- Repository standards, ADRs, and a backend CI workflow.
- SQL Server persistence with EF Core migrations.
- ASP.NET Core Identity with JWT authentication and role policies.
- Health endpoints for liveness and SQL readiness.
- Versioned loan workflow APIs for draft intake, updates, submission, role-based transitions, history, and deterministic list views.
- Intelligent assignment APIs for priority scoring, least-loaded eligible employee selection, Team Lead reassignment, and queue review.
- A role-based React interface for login, loan intake, details, queues, workflow actions, and assignment controls.
- Docker Compose support for SQL Server, API, and frontend local review.

## Target stack

- C# / .NET 10 / ASP.NET Core
- React + TypeScript + Vite
- SQL Server + EF Core in the persistence milestone
- xUnit for backend tests
- GitHub Actions and Docker Compose

## Current verification

From the repository root:

```bash
dotnet restore
dotnet format --verify-no-changes
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build --collect:"XPlat Code Coverage"
```

Frontend checks:

```bash
cd src/mortgageflow-web
npm ci
npm run lint
npm run typecheck
npm run test:run
npm run build
npm audit --audit-level=moderate
npm audit signatures
```

## Local configuration

Create a private `.env` file from placeholders:

```bash
cp .env.example .env
```

Edit `.env` with local-only values. Do not commit `.env`.

## Full Docker startup

From the repository root:

```bash
docker compose config --quiet
docker compose up -d --build
docker compose ps
```

Open:

```text
http://localhost:5173
```

Useful endpoints:

- `GET /health/live`
- `GET /health/ready`
- `POST /api/v1/auth/login`
- `GET /api/v1/auth/me`
- `POST /api/v1/loans`
- `GET /api/v1/loans?page=&pageSize=&search=&status=`
- `GET /api/v1/loans/{id}`
- `PUT /api/v1/loans/{id}`
- `POST /api/v1/loans/{id}/transitions`
- `GET /api/v1/loans/{id}/history`
- `POST /api/v1/loans/{id}/assign`
- `POST /api/v1/loans/{id}/reassign`
- `PATCH /api/v1/loans/{id}/priority`
- `GET /api/v1/queues/me`
- `GET /api/v1/queues/team`
- `GET /openapi/v1.json` in Development

Loan workflow and assignment security are enforced in the API/application layer today: users only query loans visible to their role, Team Lead controls assignment actions, visible-but-disallowed actions return `403`, and stale row versions return `409 Conflict`. SQL Server row-level security is a possible future hardening step, but it is intentionally outside the current MVP slice.

## Local developer startup

Terminal 1:

```bash
docker compose up -d sqlserver
set -a
source .env
set +a
dotnet run --project src/MortgageFlow.Api --launch-profile http
```

Terminal 2:

```bash
cd src/mortgageflow-web
npm run dev
```

Open:

```text
http://localhost:5173
```

## Operational notes

See [docs/operations](docs/operations/README.md) for full-stack startup, smoke requests, reset/reseed commands, troubleshooting, rollback notes, and branch protection guidance.

## Scope discipline

Advanced infrastructure will be implemented after the MVP is working and demo-ready.
