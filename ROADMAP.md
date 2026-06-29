# MortgageFlow Product Roadmap

## Locked MVP

MortgageFlow is a role-based mortgage workflow and intelligent assignment platform. The MVP proves:

- C# domain modeling and OOP behavior.
- Controlled loan workflow transitions.
- REST API and role authorization.
- SQL persistence and normalized data.
- Hybrid priority and assignment logic.
- Tests, CI, Docker, and portfolio documentation.

## Delivery milestones

1. Foundation: domain model, workflow tests, and basic CI.
2. Persistence and security: SQL Server, EF Core, Identity, JWT, and API standards.
3. Loan workflow: intake APIs, transitions, history, and concurrency.
4. Assignment: priority, eligibility, least-loaded assignment, and queue APIs.
5. User experience: React login, loans, details, queue, and Team Lead screen.
6. Operations: integration tests, CI quality gates, Docker, logging, and health.
7. Portfolio polish: README, diagrams, screenshots, demo, and release preparation.

## Current milestone boundary

The current milestone packages the completed MVP for public review: documentation, diagrams, screenshots, demo flow, release notes, and final verification evidence. It does not add new product features.

## Deferred scope

Document upload, advanced dashboards, administrator tooling, Kafka, Redis, microservices, cloud deployment, and database-level row security are intentionally deferred until after the MVP is complete and explainable.
