# Security Policy

MortgageFlow uses synthetic demo data only.

## Data boundaries

Do not add, seed, log, or document real borrower data. The project must not store:

- Social Security numbers
- Bank account numbers
- Credit reports
- Tax returns
- Government IDs
- Real mortgage documents

## Secrets

Do not commit JWT signing keys, SQL passwords, demo passwords, API keys, or connection strings containing credentials. Use environment variables, user-secrets, or `.env` files ignored by Git.

## Reporting issues

Record security issues privately until they can be fixed without exposing sensitive details.
