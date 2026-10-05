# DevVault

A personal code-snippet vault, built as a small but complete **Clean Architecture** reference
in .NET 10 — the kind of skeleton worth copying into a real project rather than a tutorial toy.

[![CI](https://github.com/Menzi-Thami/DevVault/actions/workflows/ci.yml/badge.svg)](https://github.com/Menzi-Thami/DevVault/actions/workflows/ci.yml)

## What it does

Stores code snippets — a title, a body, and a language — over a small REST API.

| Method | Route | Result |
|---|---|---|
| `POST` | `/api/snippets` | `201 Created` with the new snippet |
| `GET` | `/api/snippets` | all snippets |
| `GET` | `/api/snippets/{id}` | one snippet, or `404` |

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
  (`not_found`, `domain_rule_violated`, `unexpected_error`). No handler catches an exception
  just to return `null`.

Two conventions the tests depend on: nothing reads the clock statically (`TimeProvider` is
injected, so `FakeTimeProvider` can pin an instant), and nothing is resolved from a static
service locator.

## Running it

Needs the [.NET 10 SDK](https://dotnet.microsoft.com/download) and SQL Server (LocalDB is fine).
The connection string is not committed — supply `ConnectionStrings:DefaultConnection`:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=(localdb)\MSSQLLocalDB;Database=DevVault;Trusted_Connection=True;TrustServerCertificate=True" \
  --project DevVault.API

dotnet ef database update --project DevVault.Infrastructure --startup-project DevVault.API
dotnet run --project DevVault.API
```

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
