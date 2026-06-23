# MortgageFlow Product Roadmap

## Locked MVP

MortgageFlow is a role-based mortgage workflow and intelligent assignment platform. The MVP must prove:

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
7. Portfolio polish: README, diagrams, screenshots, demo, and interview preparation.

## Foundation boundary

The foundation includes a clean solution, React scaffold, repository standards, ADRs, pure domain behavior, tests, and backend CI. It does not include persistence, authentication, or product UI.
