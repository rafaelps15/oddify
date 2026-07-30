---
name: ca-review
description: Review the current diff (or a given module/path) against this modular monolith template's architectural rules — layering/dependency direction, CQRS/entity conventions, cross-module boundaries — and report violations with file:line.
---

# ca-review

"Clean/modular Architecture review." Unlike `/code-review` (general correctness +
simplification), this skill checks **only** architectural-convention compliance. Read this
repo's `CLAUDE.md` (or equivalent architecture doc) first if one exists — its documented rules
take precedence over the generic checklist below wherever they differ, since it's the
authoritative source for this specific repo. Default scope is the working diff (`git diff` /
`git diff --staged`, whichever is non-empty); if the user names a module or path, scope to
that instead. `<ProjectName>`/`<ModuleA>`/`<ModuleB>` below stand for this repo's actual
root namespace and module names.

For every finding, quote the exact file:line and the exact rule it violates (from this repo's
own conventions doc where possible) — don't flag stylistic preferences that aren't actually
documented or established conventions in this repo. If a check requires an assumption this
codebase doesn't yet establish a precedent for (e.g. a genuinely new pattern), say so
explicitly rather than inventing a rule.

## Checklist

### 1. Layering / dependency direction
- Does any changed `.csproj` add a `ProjectReference` that violates this repo's documented
  layering? In particular: **Domain** referencing anything beyond the shared domain kernel;
  **Application** referencing **Infrastructure**/**Presentation**; **Presentation**
  referencing **Infrastructure**.
- Does any changed `.cs` file in a `Domain` project `using` a namespace from EF Core,
  MediatR, FluentValidation, Dapper, or ASP.NET Core? (Domain must be pure C#.)
- Does a repository interface's **implementation** live outside `Infrastructure`, or the
  **interface** live outside `Domain`?

### 2. Cross-module boundaries
- Does any module's `Domain`/`Application`/`Infrastructure` project reference another
  module's `Domain`/`Application`/`Infrastructure`/`Presentation` project directly? Check this
  repo's documented exception(s) — typically only a dedicated integration-events/contracts
  project is a legitimate cross-module reference (e.g. a fictional `Tasks.Presentation` →
  `Users.IntegrationEvents`, following whatever this repo's real module names are).
- If new synchronous cross-module logic was added via a `PublicApi`-style contract project,
  flag it for discussion if this repo's such projects are otherwise unimplemented/unwired —
  silently making one "real" is a significant architectural decision, not a routine change.
- If a new integration event was introduced: is it defined in the *publishing* module's
  contracts project (not Application/Domain), and does the publish happen from a domain-event
  handler via an event-bus abstraction, not directly from a command handler?

### 3. CQRS conventions
- Command/Query records: `public sealed record`, suffixed `Command`/`Query`, implementing
  this repo's `ICommand`/`ICommand<T>`/`IQuery<T>`-style interfaces correctly (not a
  hand-rolled interface).
- Handlers: `internal sealed class`, suffixed `CommandHandler`/`QueryHandler`, primary-ctor DI.
- Does a **query** handler have a `Validator` class? (It shouldn't, if this repo's validation
  pipeline only runs for commands.)
- Does a **query** handler use EF (`DbContext`) instead of Dapper/a raw connection factory, if
  this repo's established pattern is Dapper for reads and EF/repository for writes? Flag it.
- Does any `*Response`-suffixed type, `Result<T>` from a cross-module/external read, or another
  externally-shaped DTO (another module's `PublicApi` contract, an external HTTP client's
  payload type) appear inside a **command** handler — as a local variable, a parameter, or
  unwrapped via `.Value`? Per this repo's `CLAUDE.md` ("Command handler shape"), that read
  belongs in a dedicated plain injected service (`Task<T?>`, nullable, registered by hand in
  `<Module>Module.AddInfrastructure`) — **not** a MediatR query sent via `ISender`, since a
  query handler's contract forces the same `Result<T>` unwrap this rule exists to avoid.
  Exception: a command's own declared return type (its `ICommand<TResponse>` generic argument,
  built at the end of `Handle`) is not a violation.
- Does a command handler call `SaveChanges`/`SaveChangesAsync` anywhere other than via the
  unit-of-work abstraction exactly once, after all mutations? Does a repository method call
  `SaveChanges` itself? (Repositories must never do this, per this template.)
- Endpoint class: `internal sealed class : IEndpoint` (or this repo's equivalent), one file
  per endpoint, ends with the standard `result.Match(...)` pattern, tagged with this module's
  `Tags` class. Flag any manual registration of the endpoint/handler/validator in DI — none
  should exist; discovery is via assembly scanning.
- Flag any new `[Authorize]`, policy, or API-version attribute if none exist in this codebase
  today — introducing one is a scope decision to confirm with the user, not a silent addition.

### 4. Domain entity conventions
- Entity: `sealed class : Entity`, private **parameterless** constructor, `private set` on every
  property, mutation only through named behavior methods (no public setters). `Create(...)`
  builds the instance via an **object initializer** — flag a parameterized private constructor
  (field assignment split between constructor args and initializer) as a deviation.
- Every state transition that changes observable state raises exactly one domain event; a
  no-op call (value unchanged) must **not** raise one — check for early-return guards.
- Are references to other aggregates stored as a bare `Guid` id, or did the change introduce
  a navigation property/direct object reference across aggregates? Flag the latter.
- Is a new `IEntityTypeConfiguration<T>` class added for an entity with **no** relationship
  to configure and **no** column-level constraint (max length, unique index, etc.) either?
  Flag as unnecessary only in that case — many entities in this kind of repo get a
  configuration class purely for column constraints even with zero relationships, so don't
  flag those as unnecessary (check a sibling entity before deciding either way).
- Domain errors: one `static class <Aggregate>Errors`, `NotFound(Guid)` method present when a
  "get or fail" path exists, correct error-factory choice and `"<Entities>.<Reason>"` code
  format (or whatever code format this repo actually uses).

### 5. Database / migrations
- Is a migration file hand-edited instead of `dotnet ef migrations add`-generated? (Compare
  the diff shape against a typical EF-generated migration — hand edits often touch only part
  of the `Up`/`Down`/snapshot trio.)
- Does the change introduce a table outside the module's own schema, or reference another
  module's schema directly in SQL/Dapper?

### 6. Style/build gates
- File-scoped namespaces used (not block-scoped)? Braces present on every `if`? Check
  `.editorconfig`/`Directory.Build.props` for whether these are `:error`-severity and will
  fail the build under warnings-as-errors regardless of this review — flag obvious violations
  early to save a build cycle.

## Output

List findings ranked most-severe first (missing layering boundary > cross-module leak >
naming/pattern drift > style nit). For each: file:line, the rule violated (quoting this
repo's own `CLAUDE.md`/conventions doc where one exists), and the concrete fix. If the diff is
clean, say so plainly — don't invent findings to fill space. If `ReportFindings` is available
in this session, use it to report; otherwise report in prose with file:line references.
