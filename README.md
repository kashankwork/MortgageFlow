# MortgageFlow

MortgageFlow is an mortgage workflow simulation.

It is scoped as an educational project. It does not claim to reproduce internal systems, proprietary workflows, or business rules.

## Current foundation

The foundation slice establishes:

- A modular-monolith .NET solution.
- A pure Domain project with guarded entities and value objects.
- A tested loan workflow state machine.
- A Vite React + TypeScript frontend scaffold.
- Repository standards, ADRs, and a backend CI workflow.

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

Frontend scaffold:

```bash
cd src/mortgageflow-web
npm install
npm run build
```

## Scope discipline

More Advanced infrastructure will be implemented after the MVP is working and demo-ready.
