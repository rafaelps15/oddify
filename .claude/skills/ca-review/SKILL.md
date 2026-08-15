---
name: ca-review
description: Review changed or existing .NET code for compliance with a Clean Architecture / Modular Monolith style — entity encapsulation, Result pattern usage, CQRS/MediatR conventions, FluentValidation placement, Dapper-for-reads/EF-for-writes separation, minimal-API endpoint shape, and module boundary isolation. Use when the user asks to review, audit, or check a PR/diff/file for architecture compliance, or asks "does this follow the architecture" / "is this correctly layered".
argument-hint: [optional: specific files or feature to review; defaults to the working-tree diff]
---

# Clean Architecture Review

Reviews code against one specific, opinionated architecture: **Modular Monolith** with per-module
`Domain` / `Application` / `Infrastructure` / `Presentation` projects, the **Result pattern** (no
exceptions for expected failures), **CQRS via MediatR**, **FluentValidation**, **Dapper for reads / EF
Core for writes**, and **minimal-API endpoints** behind an `IEndpoint` marker. This is not a generic
"is this good code" review — it's a conformance check against that specific shape. If the target repo
doesn't actually use this stack, say so and stop rather than forcing the checklist onto it.

Confirm the shape (Step 0 of `add-entity`/`add-feature`) against the repo before reviewing — resolve
`<RootNamespace>` and read the actual `Entity`, `Result`, `ICommand`/`IQuery`, `IEndpoint` base types
once, so findings cite the real types rather than assumed ones.

Work through the checklist below against the diff/file(s) in scope. For each finding, cite the exact
file and line, state which rule it violates, and give the concrete fix (a short code sketch is more
useful than a description). Don't flag things this checklist doesn't cover just because they look
unusual — stay inside the architecture's scope.

## 1. Domain layer

- [ ] **No public setters.** Every entity property is `{ get; private set; }` (or `init` for value
      objects/domain events). A `{ get; set; }` on an entity is always a finding.
- [ ] **No public constructor.** Entities have a private parameterless constructor (or `internal` for a
      child entity meant to be built only by its aggregate root); construction goes through a static
      `Create`/verb-named factory method.
- [ ] **Business logic lives on the entity, not the caller.** If a handler (or anything outside the
      entity) reads two properties and computes a decision that's really a domain rule (e.g. checking
      `Status != Draft` before allowing a transition), that logic belongs in a method on the entity, not
      inlined where it's called from.
- [ ] **State transitions raise domain events**, and only when something actually changed — a
      transition method that early-returns on a no-op input must not raise an event on that path.
- [ ] **Expected failures return `Result`/`Result<T>`, never throw.** A `throw new
      InvalidOperationException(...)` (or similar) for a business-rule violation the caller could
      reasonably trigger (not-found, already-in-that-state, invalid-input) is a finding — it should be a
      `Result.Failure(<Entity>Errors.X)`. Exceptions are reserved for truly unexpected/programmer-error
      conditions.
- [ ] **`<Entity>Errors` shape.** `NotFound(id)` (or similar parameterized lookups) is a method; every
      other error is a `static readonly Error` field. Error codes follow `<PluralEntity>.<Reason>`. The
      chosen `Error` factory (`NotFound`/`Conflict`/`Problem`/`Failure`) matches the failure's actual
      HTTP-intent — a "not found" case using `Error.Problem` (or vice versa) is a finding.
- [ ] **Repository interfaces live in Domain, not Application/Infrastructure.** An `I<Entity>Repository`
      declared outside the Domain project, or a repository method that leaks an EF/Dapper-specific type
      (`IQueryable<T>`, `DbSet<T>`, a raw `DbConnection`) into its signature, is a finding.

## 2. Application layer — commands (writes)

- [ ] **Command records are `sealed record : ICommand`/`ICommand<T>`**, positional parameters only,
      named for exactly what the caller supplies.
- [ ] **Handlers are `internal sealed`**, constructed via primary-constructor DI, and depend only on the
      entity's repository interface + `IUnitOfWork` (never `DbContext`, never Dapper, never another
      module's repository).
- [ ] **Handler is thin.** It loads (if needed) → delegates the actual rule to the entity → checks the
      returned `Result` → persists. A handler containing `if` statements that re-implement a rule already
      expressible as an entity method is a finding — even if it "works," it duplicates business logic
      outside the aggregate and will drift from it.
- [ ] **`SaveChangesAsync` is called exactly once, after every failure branch has already returned.** A
      handler that calls it before a later check that can still fail is a finding (it would persist a
      partial/invalid change).
- [ ] **Validators exist for every command**, `internal sealed : AbstractValidator<TCommand>`, and only
      validate input shape (non-empty, ranges, lengths) — a validator rule that needs a database lookup or
      encodes a business rule (uniqueness, state-dependent legality) is a finding; that check belongs in
      the handler/entity as a `Result.Failure`, not in FluentValidation.
- [ ] **No manual DI registration** for handlers/validators/endpoints if the repo relies on assembly
      scanning for them (check the module's DI/module-registration file) — a manually added
      `services.AddScoped<CreateXCommandHandler>()` or similar next to a scanned registration is either
      redundant or a sign the scan isn't picking it up (wrong assembly/accessibility) and should be fixed
      at the source, not papered over.

## 3. Application layer — queries (reads)

Every query handler in scope must match `add-feature/references/query-slice.md` (§B1–B5) **exactly** —
that file is the executable spec for this section, not just background reading. Read it before
reviewing any query handler, and cite the specific `§B_` section in each finding instead of a bare
"convention" reference.

- [ ] **Queries never go through the repository or `IUnitOfWork`.** A query handler that resolves
      `I<Entity>Repository` (loading a full EF-tracked aggregate just to read a few fields) is a finding
      — it should query via `IDbConnectionFactory` + Dapper directly.
- [ ] **SQL correctness.** Columns are aliased `AS {nameof(Response.Property)}`; only `nameof(...)`
      expressions are interpolated into the SQL string — any interpolation of a request value directly
      into the SQL text (rather than passed as a Dapper parameter) is a **SQL injection finding**, flag it
      as high severity regardless of how the rest of the review reads.
- [ ] **Not-found semantics.** Single-item queries fail with `Result.Failure<T>(<Entity>Errors.NotFound(...))`
      when nothing is found; collection queries return an empty collection instead of failing.
- [ ] **Response records are separate from Commands/Domain entities** — a query returning the entity
      type itself (leaking Domain outside Application) or reusing a Command record as a response shape is
      a finding.
- [ ] **No `Result.Success(...)`.** A query handler returns the bare value (`return todoItem;`) and lets
      the implicit conversion do the wrapping (query-slice.md §B1–B3) — explicit `Result.Success(...)` in
      a query handler is a finding, however harmless it looks.
- [ ] **No private `Row` type + `.ToResponse(...)` extension.** A parent-with-children query
      materializes directly into the final Response types via Dapper multi-mapping (`splitOn`), with the
      child collection as a mutable property outside the parent's positional constructor — never a
      separate intermediate type converted afterward (query-slice.md §B4). If you find a `<Entity>Row`
      record and a `.ToResponse(...)` extension sitting next to a query handler, that's the finding —
      point at §B4 for the fix, not just "simplify this."
- [ ] **No `foreach` in a query handler.** LINQ reshaping, the multi-mapping callback, or — when the
      operation is really a per-group dedup/first-row selection — `DISTINCT ON` in the SQL itself
      (query-slice.md §B4 rules) replaces it. `List<T>.ForEach(...)` doesn't count as `foreach` for this
      rule (it's the BCL method, used for composition/enrichment per §B5) — the `foreach` *statement* is
      what's banned.
- [ ] **No business logic decided inline.** If turning rows into a response needs an actual formula,
      threshold, or derived classification — not just renaming columns — that belongs in a shared static
      calculator (`Application/Calculo/<Name>Calculator.cs`), never inline in `Handle(...)`
      (query-slice.md Rules). A query handler orchestrates fetch → calculate → return; it never *is* the
      calculation.
- [ ] **Cross-module enrichment is always a single batch call**, never a `foreach`/loop calling another
      module's `PublicApi` once per row (an N+1 in disguise, query-slice.md §B5). If the target
      `PublicApi` doesn't have a batch method yet, adding one there is the fix — not a loop around the
      singular method.
- [ ] **Ownership/tenant scoping lives in the main query's `WHERE`**, never a separate `SELECT EXISTS
      (...)` pre-check query run just to produce a different `NotFound` before the real query
      (query-slice.md Rules, last bullet). One round-trip, one `WHERE` clause covering both "doesn't
      exist" and "isn't yours."

## 4. Presentation layer

- [ ] **Endpoints are `internal sealed : IEndpoint`**, one HTTP operation per class, logic limited to:
      build message → `sender.Send(...)` → `result.Match(...)`. Any branching, mapping, or business logic
      beyond that belongs in the handler, not the endpoint.
- [ ] **`Result` is translated via `.Match(...)`**, not manual `if (result.IsSuccess) ... else ...`
      chains re-deriving what `ApiResults.Problem`/the shared result-to-HTTP mapping already does.
- [ ] **Request DTOs are separate from Commands/Queries** — binding the MediatR message type directly as
      the request body couples the wire contract to the internal message shape; flag it even though it
      "works" today.
- [ ] **Route naming** is plural-noun-based REST (`todo-items`, `todo-items/{id}`,
      `todo-items/{id}/complete`), not verb-first (`/completeTodoItem`).

## 5. Infrastructure layer

- [ ] **Repository implementations are `internal sealed`**, and contain no logic beyond translating
      interface calls into EF Core operations (`SingleOrDefaultAsync`, `Add`) — any filtering/business
      logic here should be a finding pointing back to Application or Domain, whichever owns the rule.
- [ ] **EF configuration classes exist only where convention can't infer the mapping** (relationships,
      owned types, explicit indexes/columns) — an `IEntityTypeConfiguration<T>` that does nothing but
      restate what convention already does is unnecessary but not harmful; flag it only if asked for a
      thorough pass.
- [ ] **Module DI registration is scoped correctly** (`AddScoped` for repository/`DbContext`-backed
      services) and lives in the module's own registration extension, not scattered across the
      composition root.

## 6. Module boundaries (Modular Monolith specific)

- [ ] **No cross-module references except through a `PublicApi` project**, if the solution has that
      pattern — a module's Application/Infrastructure referencing another module's Domain/Application
      types directly (instead of going through its public interface) is a finding, and a significant one:
      it's exactly the coupling module boundaries exist to prevent.
- [ ] **No cross-module database access** — a repository or query handler in one module querying
      another module's schema/tables directly is a finding; it should go through the public API or an
      integration event instead.
- [ ] **Namespace matches physical module/folder placement** — a type under `Modules.Todos.*` used from
      inside `Modules.Orders.*` without going through Todos' `PublicApi` is the same finding as above,
      caught via `using` statements.

## Output format

Report findings grouped by the section above, most-severe first (SQL injection and cross-module
coupling outrank a missing validator). For each: file:line, the rule violated, and a concrete fix. If a
section has no findings, say so briefly rather than omitting it — that's useful signal that the section
was actually checked.
