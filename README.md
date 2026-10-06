# DevVault

A personal code-snippet vault, built as a small but complete **Clean Architecture** reference
in .NET 10 — the kind of skeleton worth copying into a real project rather than a tutorial toy.

[![CI](https://github.com/Menzi-Thami/DevVault/actions/workflows/ci.yml/badge.svg)](https://github.com/Menzi-Thami/DevVault/actions/workflows/ci.yml)

## What it does

Stores code snippets — a title, a body, and a language — over a small REST API. Every
`/api/snippets` call needs a bearer token; each user sees only their own snippets.

| Method | Route | Result |
|---|---|---|
| `POST` | `/api/snippets` | `201 Created` with the new snippet, owned by the caller |
| `GET` | `/api/snippets?pageSize=&cursor=` | a page of the caller's snippets, newest first: `{ items, nextCursor }` |
| `GET` | `/api/snippets/{id}` | one of the caller's snippets, or `404` (also for another user's id) |
| `GET` | `/health/live`, `/health/ready` | anonymous health probes |

The list is keyset-paginated: `pageSize` defaults to 20 and is capped at 100 by the server, and
list items are summaries (`id`, `title`, `language`, `createdAt`) without the body — fetch
`/api/snippets/{id}` for that. Pass `nextCursor` back as `cursor` for the next page; it is `null`
on the last one. No token gives `401`. The owner is always the token's user (`oid` claim, or a GUID `sub`) — the
body has no owner field, and one sent anyway is ignored.

## Why it's laid out this way

Four projects, dependencies pointing strictly inward:

```
DevVault.API  ──►  DevVault.Application  ──►  DevVault.Domain
      │                                            ▲
      └──────►  DevVault.Infrastructure  ──────────┘
```

- **Domain** — plain C#, no framework references. `Snippet`, the `Language` value object
  (a `record` that refuses to be constructed empty), and `DomainException`.
- **Application** — use cases as one handler per operation (`CreateSnippetHandler`,
  `GetSnippetByIdHandler`, `ListSnippetsHandler`) plus the `ISnippetRepository` port it
  defines for itself. It never references Infrastructure.
- **Infrastructure** — EF Core `AppDbContext`, the repository adapter, migrations.
- **API** — thin controllers that resolve a handler and return. Errors are mapped in one place
  by chained `IExceptionHandler`s: `KnownExceptionHandler` turns `NotFoundException` into `404`
  and `DomainException` into `400`; `UnhandledExceptionHandler` logs anything else and returns a
  generic `500`. Every error — including ASP.NET's own model-binding `400`s — is an RFC 9457
  `application/problem+json` body with a `traceId`, and ours also carry a stable `code`
  (`not_found`, `domain_rule_violated`, `validation_failed`, `unexpected_error`). No handler catches an exception
  just to return `null`.

Two conventions the tests depend on: nothing reads the clock statically (`TimeProvider` is
injected, so `FakeTimeProvider` can pin an instant), and nothing is resolved from a static
service locator.

## Running it

Needs the [.NET 10 SDK](https://dotnet.microsoft.com/download) and SQL Server (LocalDB is fine).
Nothing environment-specific is committed — supply the connection string and the token issuer
(any OIDC issuer works; for Entra ID register an app that exposes an API scope and use its
tenant and app ID URI):

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=(localdb)\MSSQLLocalDB;Database=DevVault;Trusted_Connection=True;TrustServerCertificate=True" \
  --project DevVault.API
dotnet user-secrets set "Authentication:Jwt:Authority" \
  "https://login.microsoftonline.com/<tenant-id>/v2.0" --project DevVault.API
dotnet user-secrets set "Authentication:Jwt:Audience" "api://<api-client-id>" --project DevVault.API

dotnet ef database update --project DevVault.Infrastructure --startup-project DevVault.API
dotnet run --project DevVault.API
```

Both sections are validated at startup, so a missing value stops the API at boot rather than
failing on the first request. `/health/live` (process up, no dependency checks) and
`/health/ready` (includes a database check) are anonymous, for probes.

### Calling it

Get an access token for the API's scope from the issuer (for Entra, any client allowed to call
`api://<api-client-id>/<scope>`; with the Azure CLI as a pre-authorised client:
`az account get-access-token --scope api://<api-client-id>/<scope> --query accessToken -o tsv`),
then send it as a bearer token:

```bash
curl -H "Authorization: Bearer $TOKEN" http://localhost:5109/api/snippets
```

`DevVault.API/DevVault.API.http` has the same calls; paste the token into its `@token` variable.

## Observability

Traces, metrics and logs go through OpenTelemetry: incoming requests (ASP.NET Core), outgoing
`HttpClient` calls, every SQL query EF Core sends (SqlClient, as a child span of the request that
caused it), and runtime metrics (GC, thread pool, allocations). Health probes are not traced. The
`service.name` is `Observability:ServiceName` (default `devvault-api`, validated at startup).

Nothing is exported unless you say where — the standard variables decide, so a machine with
neither set needs no collector and logs nothing about it:

| Set | Sends to |
|---|---|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | any OTLP endpoint (the Aspire dashboard, an OpenTelemetry Collector, Jaeger, ...) |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Azure Monitor / Application Insights |

Both can be set at once. To see traces locally, run the standalone
[Aspire dashboard](https://learn.microsoft.com/dotnet/aspire/fundamentals/dashboard/standalone)
(a container, or `dotnet tool`) and point the API at its OTLP port:

```bash
docker run --rm -p 18888:18888 -p 4317:18889 mcr.microsoft.com/dotnet/aspire-dashboard:latest
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317 dotnet run --project DevVault.API
```

then open http://localhost:18888 (the container prints a login token on startup).

**Correlation.** Every response has an `X-Trace-Id` header: the request's W3C trace id
(continuing the caller's `traceparent` if it sent one). The same value is the `traceId` in every
ProblemDetails body, the `TraceId` on every log record written during the request, and the
operation id in Application Insights — so a client that reports an error with its `X-Trace-Id`
leads straight to the logs and spans. The console logger shows it when scopes are on
(`Logging__Console__FormatterOptions__IncludeScopes=true`).

## Tests

```bash
dotnet test DevVault.sln
```

Three test projects, all run on every push and pull request via GitHub Actions:

- **DevVault.UnitTests** — xUnit, Shouldly, NSubstitute, `FakeTimeProvider`; the domain
  invariants and each handler against a substituted repository.
- **DevVault.IntegrationTests** — the real HTTP pipeline (`WebApplicationFactory<Program>`) over
  a real SQL Server database: EF translation, the migrations, error mapping and the HTTP
  contract. The database is rebuilt from the migrations once per run and emptied between tests
  with Respawn. Locally it uses LocalDB (`DevVault_IntegrationTests`); set `DEVVAULT_TEST_SQL` to
  a connection string to point it elsewhere — CI points it at a SQL Server service container.
- **DevVault.ArchitectureTests** — ArchUnitNET rules for the inward-only layering above, plus a
  check that Domain and Application reference neither EF Core nor ASP.NET Core.

## Licence

[MIT](LICENSE).
