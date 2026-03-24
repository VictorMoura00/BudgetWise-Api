# BudgetWise — Web API

## Project Context

BudgetWise is a .NET 10 REST API using Clean Architecture with JWT authentication.

## Tech Stack

- **.NET 10** / C# 14
- **ASP.NET Core Minimal APIs** — `IEndpointGroup` per feature with `app.MapEndpoints()` auto-discovery
- **Entity Framework Core** — PostgreSQL via Npgsql
- **FluentValidation** — request validation
- **Mapster** — object mapping
- **Serilog** — structured logging (Seq sink)
- **Scalar** — OpenAPI UI
- **JWT Bearer** — authentication
- **xUnit** + **Testcontainers** — integration and unit testing

## Architecture

Clean Architecture with the following project structure:

```
src/
  BudgetWise.Domain/         # Entities, enums, interfaces, domain logic (no dependencies)
  BudgetWise.Application/    # Use cases, DTOs, validation (references Domain)
  BudgetWise.Infrastructure/ # EF Core, external services (references Application + Domain)
  BudgetWise.Api/            # Endpoints, middleware (references all)
tests/
  BudgetWise.IntegrationTests/
  BudgetWise.UnitTests/
```

## Coding Standards

- **C# 14 features** — primary constructors, collection expressions, `field` keyword, records, pattern matching
- **File-scoped namespaces** — always
- **`var` for obvious types** — use explicit types when not clear from context
- **Naming** — PascalCase for public members, `_camelCase` for private fields, suffix async methods with `Async`
- **No regions** — ever
- **No comments for obvious code** — only comment "why", never "what"
- **Sealed classes** — seal handlers and internal implementations by default

## Skills

Load these dotnet-claude-kit skills for context:

- `modern-csharp` — C# 14 language features and idioms
- `clean-architecture` — Layered project structure with dependency inversion
- `minimal-api` — Endpoint routing, TypedResults, OpenAPI metadata
- `ef-core` — DbContext patterns, query optimization, migrations
- `testing` — xUnit, WebApplicationFactory, Testcontainers
- `error-handling` — Result pattern, ProblemDetails
- `authentication` — JWT/OIDC configuration
- `logging` — Serilog, OpenTelemetry
- `configuration` — Options pattern, secrets management
- `dependency-injection` — Service registration patterns
- `workflow-mastery` — Parallel worktrees, verification loops, subagent patterns
- `self-correction-loop` — Capture corrections as permanent rules in MEMORY.md
- `wrap-up-ritual` — Structured session handoff to `.claude/handoff.md`
- `context-discipline` — Token budget management, MCP-first navigation

## Commands

```bash
# Build
dotnet build

# Run (development)
dotnet run --project src/BudgetWise.Api

# Run tests
dotnet test

# Add EF migration
dotnet ef migrations add [Name] --project src/BudgetWise.Infrastructure --startup-project src/BudgetWise.Api

# Apply migrations
dotnet ef database update --project src/BudgetWise.Infrastructure --startup-project src/BudgetWise.Api

# Format check
dotnet format --verify-no-changes
```

## Workflow

- **Plan first** — Enter plan mode for any non-trivial task (3+ steps or architecture decisions). Iterate until the plan is solid before writing code.
- **Verify before done** — Run `dotnet build` and `dotnet test` after changes. Ask: "Would a staff engineer approve this?"
- **Fix bugs autonomously** — When given a bug report, investigate and fix it without hand-holding. Check logs, errors, failing tests — then resolve them.
- **Stop and re-plan** — If implementation goes sideways, STOP and re-plan. Don't push through a broken approach.
- **Use subagents** — Offload research, exploration, and parallel analysis to subagents.
- **Learn from corrections** — After any correction, capture the pattern in memory so the same mistake never recurs.

## Anti-patterns

Do NOT generate code that:

- Defines endpoints in Program.cs — use `IEndpointGroup` per feature
- Uses `DateTime.Now` — use `TimeProvider` injection instead
- Creates `new HttpClient()` — use `IHttpClientFactory`
- Uses `async void` — always return `Task`
- Blocks with `.Result` or `.Wait()` — await instead
- Uses `Results.Ok()` — use `TypedResults.Ok()` for OpenAPI
- Returns domain entities from endpoints — always map to response DTOs
- Creates repository abstractions over EF Core — use DbContext directly
- Uses in-memory database for tests — use Testcontainers
- Catches bare `Exception` — catch specific types, let the global handler catch the rest
- Uses string interpolation in log messages — use structured logging templates
