# ADR-002: SQL Server and EF Core

## Status

Accepted

## Context

The project should map naturally to UWM-adjacent enterprise .NET expectations and prove SQL-backed application development.

## Decision

MortgageFlow will use SQL Server with EF Core in the persistence milestone.

## Consequences

- The Domain project remains persistence-free.
- Migrations, indexes, row-version concurrency, and normalized relationships become visible portfolio evidence.
- Local setup will use Docker Compose for repeatable development.
