# ADR-003: Explicit Services and DTOs

## Status

Accepted

## Context

MortgageFlow should keep business behavior clear. Generic abstractions can hide the workflow rules that the application depends on.

## Decision

Use explicit application services and DTOs. Do not add MediatR or a generic repository unless the product develops a concrete need for them.

## Consequences

- Code paths stay direct and easy to explain.
- Business rules remain visible in named services.
- Additional patterns may be introduced later only when they reduce real complexity.
