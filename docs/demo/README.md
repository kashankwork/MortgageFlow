# Product Walkthrough

This guide walks through the core MortgageFlow workflow: secure loan intake, SQL persistence, deterministic assignment, and role-based review.

## Setup

Start the local stack:

```bash
docker compose config --quiet
docker compose up -d --build
docker compose ps
```

Open:

```text
http://localhost:5173
```

Use the synthetic accounts from the README. The shared demo password comes from the local `.env` value for `Seed__DemoPassword`.

## Workflow

### 1. Product story

MortgageFlow is a mortgage workflow simulation. A broker creates a synthetic loan, the backend validates and persists it, a Team Lead assigns work based on eligibility and workload, and processors/underwriters move the file through a controlled workflow.

### 2. Broker loan intake

- Sign in as `broker@example.test`.
- Open the loan list and create a new loan.
- Draft loans can be incomplete, while submission requires borrower, property, amount, purpose, rate, and term.
- Submitting a complete loan creates workflow history and prepares the loan for assignment.

### 3. Team Lead assignment

- Sign in as `teamlead@example.test`.
- Open the team queue and the submitted loan.
- Use auto-assign.
- Assignment uses required role/skill, active and available employees, team match, remaining capacity, normalized load, and round-robin tie-breaking.
- Priority updates and manual reassignment require a reason and are recorded for auditability.

### 4. Processor queue work

- Sign in as the assigned processor.
- Open My Queue.
- Move the assigned loan from Submitted to Processing, then to Underwriting when complete.
- Invalid transitions fail without writing misleading history or audit rows.

### 5. Underwriter decision

- Sign in as an underwriter with assigned underwriting work.
- Open My Queue.
- Approve or reject the loan.
- The status timeline shows persisted history, audit, assignment, and row-version updates.

### 6. Engineering checks

- Architecture notes describe the modular monolith boundary.
- GitHub Actions validates backend tests, frontend checks, and Docker image builds.
- SQL-backed integration tests, frontend tests, package audit/signature checks, and Docker Compose support repeatable review.

## Technical notes

- Domain rules live in the Domain project; EF Core and ASP.NET dependencies stay out of it.
- Controllers are thin and delegate workflow behavior to application-facing services.
- API authorization is enforced server-side. The frontend hides unavailable actions only for usability.
- SQL Server row versions return `409 Conflict` for stale edits.
- Assignment is deterministic and testable because priority, eligibility, workload, and tie-breaking are separated.
- Docker Compose provides a reproducible SQL/API/frontend review environment.
