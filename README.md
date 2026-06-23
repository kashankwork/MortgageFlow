# MortgageFlow

MortgageFlow is an interview-ready mortgage workflow simulation built for a United Wholesale Mortgage software developer portfolio conversation.

It is intentionally scoped as an educational product. It does not claim to reproduce UWM internal systems, proprietary workflows, or business rules.

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

A smaller, complete, tested application is stronger than an unfinished app with Kafka, Redis, microservices, Kubernetes, or AI features. Advanced infrastructure is deferred until after the interview MVP is working and demo-ready.
