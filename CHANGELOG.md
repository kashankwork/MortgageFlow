# Changelog

All notable release-ready changes are summarized here.

## v1.0.0 - Prepared

### Added

- Role-based mortgage workflow simulation with Broker, Processor, Underwriter, and Team Lead paths.
- SQL Server persistence with EF Core migrations, Identity tables, loan workflow tables, assignment records, status history, audit logs, row-version concurrency, and health readiness checks.
- JWT authentication, role policies, versioned API routes, Problem Details responses, and OpenAPI bearer security.
- Loan intake, draft update, detail/list/history, status transition, assignment, reassignment, priority, personal queue, and team queue APIs.
- Deterministic assignment engine using priority scoring, eligibility filters, normalized workload, and round-robin tie-breaking.
- React + TypeScript frontend with in-memory JWT auth, role-aware navigation, loan screens, queue screens, workflow actions, and Team Lead controls.
- SQL-backed integration tests, frontend tests, package audit/signature checks, Docker image builds, and GitHub Actions quality gates.
- Multi-container Docker Compose stack for SQL Server, API, and frontend.
- Correlation-aware request logging and audit correlation IDs.
- Public documentation, architecture diagrams, demo guide, screenshot index, operations notes, and release notes.

### Security and data boundaries

- Real secrets are externalized through local environment configuration.
- `.env` values, JWT signing keys, SQL passwords, demo passwords, tokens, local notes, and private prep files are ignored.
- Demo users and loans use synthetic data only.
- Audit summaries avoid borrower names, borrower emails, income values, and property details.

### Known limitations

- SQL Server row-level security is not enabled; visibility is enforced in application queries and services.
- The frontend stores the access token in React memory only, so browser refresh requires signing in again.
- Deployment, document upload, administrator management screens, and external integrations are intentionally out of scope.
- `v1.0.0` should be tagged only after the final release-packaging PR is merged and CI is green.
