# ADR-002: SQL Server and EF Core

## Status

Accepted

## Context

The project should map naturally to enterprise .NET expectations and prove SQL-backed application development.

## Decision

MortgageFlow uses SQL Server with EF Core for persistence.

## Consequences

- The Domain project remains persistence-free.
- Migrations, indexes, row-version concurrency, and normalized relationships are part of the implementation.
- Local setup will use Docker Compose for repeatable development.
