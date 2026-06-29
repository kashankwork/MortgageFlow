# Product Walkthrough

This guide walks through the core MortgageFlow workflow: secure loan intake, SQL persistence, deterministic assignment, and role-based review.

## Setup

Start from a clean local Docker database when capturing screenshots or running the full workflow:

```bash
docker compose config --quiet
docker compose down -v
docker compose up -d --build
docker compose ps
```

Open:

```text
http://localhost:5173
```

Use the synthetic accounts from the README. The shared demo password comes from your private `.env` value for `Seed__DemoPassword`.

## Walkthrough

### 1. Product story

MortgageFlow is a mortgage workflow simulation. A broker creates a synthetic loan, the backend validates and persists it, a Team Lead assigns work based on eligibility and workload, and processors/underwriters move the file through a controlled workflow.

### 2. Broker loan intake

- Sign in as `broker@example.test`.
- Open the loan list and create a new loan.
- Point out that draft loans can be incomplete, but submission requires borrower, property, amount, purpose, rate, and term.
- Submit a complete loan and note that row-version concurrency protects later updates.

### 3. Team Lead assignment

- Sign in as `teamlead@example.test`.
- Open the team queue and the submitted loan.
- Use auto-assign.
- Explain the assignment decision: required role/skill, active and available employee, team match, remaining capacity, normalized load, and round-robin tie-breaking.
- Optionally update priority or reassign with a reason to show audit-friendly operations.

### 4. Processor queue work

- Sign in as the assigned processor.
- Open My Queue.
- Move the assigned loan from Submitted to Processing, then to Underwriting when complete.
- Mention that invalid transitions fail without writing misleading history or audit rows.

### 5. Underwriter decision

- Sign in as an underwriter with assigned underwriting work.
- Open My Queue.
- Approve or reject the loan.
- Show the status timeline and explain that history, audit, assignment, and row-version updates are persisted.

### 6. Engineering checks

- Open the architecture docs and explain the modular monolith boundary.
- Open GitHub Actions and show backend, frontend, and Docker image build gates.
- Mention SQL-backed integration coverage, frontend tests, package audit/signature checks, and Docker reproducibility.

## Technical notes

- Domain rules live in the Domain project; EF Core and ASP.NET dependencies stay out of it.
- Controllers are thin and delegate workflow behavior to application-facing services.
- API authorization is enforced server-side. The frontend hides unavailable actions only for usability.
- SQL Server row versions return `409 Conflict` for stale edits.
- Assignment is deterministic and testable because priority, eligibility, workload, and tie-breaking are separated.
- Docker Compose provides a reproducible SQL/API/frontend review environment.

## Screenshot checklist

- Login and role navigation.
- Loan list and create/edit form.
- Loan detail with status, priority, borrower/property summary, and timeline.
- Team Lead assignment controls and result.
- My Queue for processor or underwriter.
- OpenAPI JSON or Swagger/API contract view.
- GitHub Actions green checks.

Do not capture personal browser tabs, email inboxes, desktop notifications, terminal output containing secrets, tokens, or local `.env` values.
