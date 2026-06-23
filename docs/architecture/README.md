# Architecture Notes

MortgageFlow starts as a modular monolith:

```text
React/Vite UI -> ASP.NET Core API -> Application Services -> Domain
                                      Infrastructure -> SQL Server
```

The Domain project owns core workflow rules and invariants. Infrastructure will adapt those rules to EF Core and SQL Server in the persistence milestone.
