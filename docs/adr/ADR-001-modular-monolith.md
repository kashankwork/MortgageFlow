# ADR-001: Modular Monolith

## Status

Accepted

## Context

MortgageFlow needs clean architecture and business behavior without unnecessary distributed-system overhead.

## Decision

MortgageFlow will use a modular-monolith structure with separate Domain, Application, Infrastructure, API, and frontend projects.

## Consequences

- The solution remains easy to run, review, and maintain.
- Boundaries are explicit through project references.
- Microservices can be extracted later only if the product needs that complexity.
