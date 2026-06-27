# MortgageFlow

MortgageFlow is a mortgage workflow simulation.

It is scoped as an educational project. It does not claim to reproduce internal systems, proprietary workflows, or business rules.

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
dotnet test --configuration Release --no-build
```

SQL-backed integration tests require a local SQL Server container:

```bash
cp .env.example .env
# Edit .env and choose a strong local-only SQL password.
docker compose up -d

export MORTGAGEFLOW_SQL_PASSWORD="<same value from .env>"
dotnet test --configuration Release
```

Apply the initial migration locally:

```bash
export ConnectionStrings__DefaultConnection="Server=127.0.0.1,14333;Database=MortgageFlow;User Id=sa;Password=<local password>;TrustServerCertificate=True;Encrypt=True"
export Jwt__SigningKey="<at least 32 characters for local development>"
export Seed__DemoPassword="<synthetic demo password>"
dotnet ef database update --project src/MortgageFlow.Infrastructure --startup-project src/MortgageFlow.Api
```

Run the API locally after setting configuration through user-secrets or environment variables:

```bash
dotnet run --project src/MortgageFlow.Api
```

Useful endpoints:

- `GET /health/live`
- `GET /health/ready`
- `POST /api/v1/auth/login`
- `GET /api/v1/auth/me`
- `GET /openapi/v1.json` in Development

Frontend scaffold:

```bash
cd src/mortgageflow-web
npm install
npm run build
```

## Scope discipline

Advanced infrastructure will be implemented after the MVP is working and demo-ready.
