# ADR-003: Explicit Services and DTOs

## Status

Accepted

## Context

The MVP should be understandable in a short interview walkthrough. Generic abstractions can hide the business rules that this project is meant to showcase.

## Decision

Use explicit application services and DTOs. Do not add MediatR or a generic repository unless the MVP develops a concrete need for them.

## Consequences

- Code paths stay direct and easy to explain.
- Business rules remain visible in named services.
- Additional patterns may be introduced later only when they reduce real complexity.
