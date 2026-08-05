---
name: ca-review
description: Review the current diff (or a given module/path) against this modular monolith template's architectural rules — layering/dependency direction, CQRS/entity conventions, cross-module boundaries — and report violations with file:line.
---

# ca-review

"Clean/modular Architecture review." Unlike a general `/code-review` (correctness +
simplification), this skill checks **only** architectural-convention compliance. Read this
repo's root `CLAUDE.md` first if one exists — its documented rules take precedence over the
generic checklist below wherever they differ, since it's the authoritative source for this
specific repo. Default scope is the working diff (`git diff` / `git diff --staged`, whichever is
non-empty); if the user names a module or path, scope to that instead.
`<ProjectName>`/`<ModuleA>`/`<ModuleB>` below stand for this repo's actual root namespace and
module names.

For every finding, quote the exact file:line and the exact rule it violates (from this repo's own
conventions doc where possible) — don't flag stylistic preferences that aren't actually documented
or established conventions in this repo. If a check requires an assumption this codebase doesn't
yet establish a precedent for (a genuinely new pattern), say so explicitly rather than inventing a
rule. Several checks below intentionally call out places where this style of codebase can end up
inconsistent (§6 in this repo's `CLAUDE.md`) — don't flag those as violations when the diff simply
follows one of the two existing shapes already present in this repo; only flag a **third**,
unprecedented shape.

## Checklist

### 1. Layering / dependency direction
- Does any changed `.csproj` add a `ProjectReference` that violates this repo's documented
  layering? In particular: **Domain** referencing anything beyond `Common.Domain`;
  **Application** referencing **Infrastructure**/**Presentation**; **Presentation** referencing
  **Infrastructure**.
- Does any changed `.cs` file in a `Domain` project `using` a namespace from EF Core, MediatR,
  FluentValidation, Dapper, or ASP.NET Core? (Domain must be pure C#, only depending on
  `Common.Domain`.)
- Does a repository interface's **implementation** live outside `Infrastructure`, or the
  **interface** live outside `Domain`?
- Does a module's `Application` project define its own `IUnitOfWork` (in
  `Abstractions/Data/IUnitOfWork.cs`), rather than pulling in a shared one from `Common`? Flag a
  new module that tries to reuse another module's `IUnitOfWork` type — each module owns its own.

### 2. Cross-module boundaries
- Does any module's `Domain`/`Application`/`Infrastructure` project reference another module's
  `Domain`/`Application`/`Infrastructure`/`Presentation` project directly? The only legitimate
  cross-module reference in this template is `<ModuleA>.Presentation` → `<ModuleB>.IntegrationEvents`
  (never `<ModuleB>.Domain`/`Application`/`Infrastructure`/`Presentation`).
- If new synchronous cross-module logic was added via a `PublicApi`-style contract project, flag
  it for explicit discussion — this repo's `PublicApi` projects are reserved/unimplemented by
  default (not in the `.sln`, no registered implementation); silently making one "real" is a
  significant architectural decision, not a routine change. Verify the actual current state of
  this repo's `PublicApi` projects before asserting either way.
- If a new integration event was introduced: is it defined in the *publishing* module's own
  `IntegrationEvents` project (not Application/Domain)? Does the publish happen from a
  domain-event handler via the `IEventBus` abstraction — not directly from a command handler, and
  not from inside the entity itself?
- Does a domain-event handler that publishes an integration event escalate a `Result.Failure` by
  throwing (this template's `AppException`), rather than swallowing it silently? A notification
  handler has no `Result` return channel — silent failure here means a cross-module event
  silently never fires.

### 3. CQRS conventions
- Command/Query records: `public sealed record`, suffixed `Command`/`Query`, implementing this
  repo's `ICommand`/`ICommand<T>`/`IQuery<T>`-style interfaces correctly (not a hand-rolled
  interface).
- Handlers: `internal sealed class`, suffixed `CommandHandler`/`QueryHandler`, primary-ctor DI.
- Does a **query** handler have a `Validator` class? (It shouldn't — this repo's validation
  pipeline behavior only runs for `IBaseCommand`.)
- Does a **query** handler use EF (`DbContext`) instead of Dapper/`IDbConnectionFactory`? Flag
  it — this template keeps writes on EF/repository and reads on Dapper, and blurring that split
  is a real regression even though nothing stops it from compiling.
- Does a paginated/filtered query recompute `Skip`/the filter predicate separately for its "get
  page" and "count" statements instead of sharing one parameters object? Flag the duplication
  risk even if the values happen to currently match.
- Does a command handler call `SaveChanges`/`SaveChangesAsync` anywhere other than via the
  `IUnitOfWork` abstraction, exactly once, after all mutations? Does a repository method call
  `SaveChanges` itself? (Repositories must never do this in this template.)
- Endpoint class: `internal sealed class : IEndpoint`, one file per endpoint, ends with
  `result.Match(...)`, tagged with this module's `Tags` class. Flag any manual registration of
  the endpoint/handler/validator in DI — none should exist; discovery is via assembly scanning
  (`AddEndpoints`/`AddMediatR`/`AddValidatorsFromAssemblies`).
- Flag any new `[Authorize]`, auth policy, or API-version attribute if this codebase has none of
  those wired up today (check for `AddAuthentication`/`AddAuthorization` actually being called,
  not just the presence of an isolated `.AllowAnonymous()`, which can be vestigial in this
  template — see this repo's `CLAUDE.md` §15) — introducing real authorization is a scope
  decision to confirm with the user, not a silent addition.

### 4. Domain entity conventions
- Entity: `sealed class : Entity`, private constructor, `private set` on every property, mutation
  only through named behavior methods (no public setters, no collection exposed as a mutable
  `List<T>`).
- Every state transition that changes observable state raises exactly one domain event; a no-op
  call (value unchanged) must **not** raise one — check for an early-return guard before
  `Raise(...)`.
- If a domain event **class** is added, is it actually `Raise(...)`d from somewhere, and — if the
  intent is cross-module visibility — does a domain-event handler exist to act on it? A defined
  but never-raised event class is a known failure mode in this style of codebase
  (`CLAUDE.md` §8/§15) — flag one exactly the same way.
- Are references to other aggregates stored as a bare `Guid` id, or did the change introduce a
  navigation property/direct object reference across aggregates (even within the same module)?
  Flag the latter.
- Is a new `IEntityTypeConfiguration<T>` class added for an entity with **no** relationship to
  configure and **no** column-level constraint (max length, unique index, etc.) either? Flag as
  unnecessary only in that specific case — many entities in this kind of repo get a configuration
  class purely for column constraints even with zero relationships, so don't flag those (check a
  sibling entity before deciding either way).
- Domain errors: one `static class <Aggregate>Errors`, `NotFound(Guid)` method present when a
  "get or fail" path exists, correct error-factory choice (`Problem` for business rules,
  `Conflict` for uniqueness/concurrency, `NotFound` only for id lookups, `Failure` only for truly
  generic failures), `"<PluralAggregate>.<PascalCaseReason>"` code format, and no aggregate
  reusing another aggregate's `*Errors` class.

### 5. Database / migrations
- Is a migration file hand-edited instead of `dotnet ef migrations add`-generated? (Compare the
  diff shape against a typical EF-generated migration — hand edits often touch only part of the
  `Up`/`Down`/snapshot trio, or are missing the `.Designer.cs` update entirely.)
- Does the change introduce a table outside the module's own schema, or reference another
  module's schema directly in SQL/Dapper?
- Does the migration's `MigrationsHistoryTable` call point at that module's own `Schemas.<Module>`
  constant, not a hardcoded string or another module's schema?

### 6. Non-EF / Redis-backed state (if touched)
- If the change adds or modifies a Redis-backed (cache-only) aggregate (this template's `Cart`-
  style pattern, see `CLAUDE.md` §13): does it correctly have **no** `Entity` base class, **no**
  repository, **no** domain events, and go through `ICacheService` exclusively, registered as a
  module-internal service (not an interface with a DI abstraction, since there's exactly one
  implementation)? Flag an attempt to give it an EF-backed repository or a domain event — that
  would silently duplicate the persistence story for the same data.

### 7. Style/build gates
- File-scoped namespaces used (not block-scoped)? Braces present on every `if`? Check
  `.editorconfig`/`Directory.Build.props` for whether these are `:error`-severity and will fail
  the build under warnings-as-errors regardless of this review — flag obvious violations early to
  save a build cycle.

## Output

List findings ranked most-severe first (missing layering boundary > cross-module leak >
naming/pattern drift > style nit). For each: file:line, the rule violated (quoting this repo's
own `CLAUDE.md`/conventions doc where one exists), and the concrete fix. If the diff is clean,
say so plainly — don't invent findings to fill space. If `ReportFindings` is available in this
session, use it to report; otherwise report in prose with file:line references.
