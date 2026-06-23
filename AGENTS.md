# Agent Instructions

## Mission

Build MortgageFlow as an interview-ready full-stack portfolio project for a software developer role.

## Scope discipline

Implement one milestone of the execution plan at a time. Stop after the requested milestone and return a completion report.

## Architecture constraints

- Domain must stay pure C# with no EF Core, ASP.NET Core, file-system, Kafka, or Redis dependencies.
- Application may reference Domain.
- Infrastructure may reference Application and Domain.
- API may reference Application and Infrastructure.
- Keep persistence, authentication, JWT, loan API endpoints, and product UI out of the foundation milestone.

## Deferred until after MVP

Kafka, Redis, microservices, Kubernetes, AI features, broad E2E suites, mutation testing, and advanced observability.
