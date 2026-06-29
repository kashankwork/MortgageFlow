# MortgageFlow Roadmap

## Current scope

MortgageFlow is a role-based mortgage workflow and intelligent assignment platform. The current version includes:

- C# domain modeling and OOP behavior.
- Controlled loan workflow transitions.
- REST API and role authorization.
- SQL persistence and normalized data.
- Hybrid priority and assignment logic.
- Tests, CI, Docker, and operational documentation.

## Included capabilities

- Secure synthetic user authentication with role-based authorization.
- Loan intake, draft update, submission, workflow transitions, and status history.
- Team assignment using eligibility, workload, priority, capacity, and round-robin tie-breaking.
- Processor, underwriter, broker, and team lead views in the React frontend.
- SQL-backed integration tests, frontend tests, Docker Compose, and CI quality gates.

## Current boundary

The current version focuses on the core workflow and assignment system. It does not include document upload, external integrations, administrator tooling, or cloud deployment.

## Deferred scope

Document upload, advanced dashboards, administrator tooling, Kafka, Redis, microservices, cloud deployment, and database-level row security are intentionally deferred.
