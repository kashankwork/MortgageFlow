# Operations Notes

These commands run MortgageFlow locally with synthetic data.

## Full Docker startup

Create a private `.env` file from the placeholder template and choose strong local-only values:

```bash
cp .env.example .env
```

Start the complete stack:

```bash
docker compose config --quiet
docker compose up -d --build
docker compose ps
```

`docker compose config --quiet` validates the file without printing resolved values. Plain `docker compose config` expands local environment values, so avoid sharing that output.

Open the frontend:

```text
http://localhost:5173
```

Health checks:

```bash
curl http://localhost:5123/health/live
curl http://localhost:5123/health/ready
```

## Smoke login

Load local secrets into the shell:

```bash
set -a
source .env
set +a
```

Login and call a protected endpoint:

```bash
TOKEN=$(curl -s -X POST http://localhost:5123/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"teamlead@example.test\",\"password\":\"$Seed__DemoPassword\"}" \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['accessToken'])")

curl http://localhost:5123/api/v1/queues/team \
  -H "Authorization: Bearer $TOKEN"
```

Treat the token like a temporary password.

## Reset and reseed local data

Use this when synthetic seed data changes and the local SQL volume still has older records:

```bash
docker compose down -v
docker compose up -d --build
```

The `-v` flag deletes only the local Docker SQL volume. It does not delete source code, commits, migrations, or GitHub data.

## Common recovery

- Compose says a required variable is missing: confirm `.env` exists at the repository root and contains the placeholder keys from `.env.example`.
- API health is not ready: wait for SQL Server to become healthy, then check `docker compose logs api`.
- Frontend opens but login fails: verify API readiness and confirm the password comes from private `.env`, not source code.
- Need to return to the previous code version: use Git to check out the previous commit or branch; do not edit generated Docker data manually.
