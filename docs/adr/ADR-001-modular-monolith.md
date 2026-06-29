# ADR-001: Modular Monolith

## Status

Accepted

## Context

The MVP needs to demonstrate clean architecture and business behavior without unnecessary distributed-system overhead.

## Decision

MortgageFlow will use a modular-monolith structure with separate Domain, Application, Infrastructure, API, and frontend projects.

## Consequences

- The solution remains easy to run and explain in a short technical walkthrough.
- Boundaries are explicit through project references.
- Microservices can be extracted later only if the MVP needs that complexity.
