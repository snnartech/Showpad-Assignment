# Lumen – local setup (Staff Full-Stack take-home)

.NET 10, controller-based APIs, Clean Architecture. The bugs described in the assignment are **intentionally still in the code**.

## Run

```bash
docker compose up -d                      # SQL Server, Redis, LocalStack (SQS), Mailpit, Aspire dashboard, nginx

# one terminal per project (any order; each one creates the DB/schema/seed data + SQS queues itself)
dotnet run --project src/Lumen.Auth.Api       # :5001
dotnet run --project src/Lumen.Api            # :5006
dotnet run --project src/Lumen.Admin.Api      # :5003
dotnet run --project src/Lumen.Scim.Api       # :5007
dotnet run --project src/Lumen.Internal.Api   # :5009
dotnet run --project src/Lumen.Queue          # :5010  (SQS consumer + Hangfire, dashboard at /hangfire)

# optional: everything under the Aspire AppHost instead of one terminal each
dotnet run --project src/Lumen.AppHost
```

| What | URL |
|---|---|
| Aspire dashboard (OpenTelemetry) | http://localhost:18888 |
| Mailpit (completion emails) | http://localhost:8026 |
| Hangfire dashboard | http://localhost:5010/hangfire |
| API docs (Scalar), per service | http://localhost:5006/scalar (and :5001, :5003, :5007, :5009) |
| nginx proxy | http://acme.lumen.localhost:8088/api/... |

## Try it

```bash
TOKEN=$(curl -s localhost:5001/auth/token -H 'content-type: application/json' \
  -d '{"email":"alice@acme.test","password":"Lumen#2026"}' | python3 -c 'import sys,json;print(json.load(sys.stdin)["access_token"])')

curl -s localhost:5006/api/leaderboard/903/top -H "authorization: Bearer $TOKEN"     # own board
curl -s localhost:5006/api/leaderboard/905/top -H "authorization: Bearer $TOKEN"     # org 77's board (LMN-4471)
curl -s -XPOST localhost:5006/api/quiz/attempt -H "authorization: Bearer $TOKEN" \
  -H 'content-type: application/json' -d '{"quizId":1,"score":92}'                   # -> SQS -> Lumen.Queue
```

Seed users (password `Lumen#2026`): `alice@acme.test`, `bob@acme.test`, `carol@acme.test`, `admin@acme.test` (admin), `scim@acme.test` (SCIM), `jpatel@globex.test`, `erin@globex.test`.

## Layout

```
src/Lumen.Domain          entities
src/Lumen.Application     use cases, interfaces (repositories, cache, email, queue), RecalculateLeaderboardTask
src/Lumen.Infrastructure  SQL Server/Dapper, Redis, SQS (LocalStack), SMTP, Hangfire, signing key store, DB init + seed
src/Lumen.Auth            authentication + authorization (RS256 JWT issue/validate, policies, internal-key scheme)
src/Lumen.ServiceDefaults Aspire service defaults (OpenTelemetry, health checks)
src/Lumen.AppHost         Aspire AppHost (optional)
src/Lumen.*.Api, Queue    hosts: Api, Auth.Api, Admin.Api, Scim.Api, Internal.Api, Queue (worker)
```
