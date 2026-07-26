---
name: code-cleanup
description: Perform a comprehensive repository-wide audit and cleanup for .NET projects following this Clean Architecture template. Apply safe, mechanical improvements automatically, and report behavioral or architectural changes separately for user approval.
argument-hint: [optional: project, module, layer, feature, or folder; defaults to the entire repository]
---

# Comprehensive Repository Cleanup

Unlike `ca-review`, which reviews only the current diff for architectural compliance, this skill audits the **entire repository**.

This skill **complements** `ca-review` rather than replacing it.

First, execute every architectural and convention check performed by `ca-review` across the full codebase. Then perform the additional cleanup and consistency checks described below.

---

# Workflow

## 1. Discover the solution

Before making changes:

- Locate the solution (`.sln` or `.slnx`) automatically.
- Explore every project.
- Identify the architectural structure.
- Identify modules and feature slices.
- Detect build and test commands.
- Read `CLAUDE.md` (or equivalent project conventions) before auditing. Repository-specific conventions always take precedence over generic rules.

Never assume the repository structure.

---

## 2. Run a full architectural audit

Execute the complete `ca-review` checklist across the entire repository instead of only the current diff.

Audit every project, module, feature and layer.

Verify every finding before acting on it.

Never trust automated findings without reading the relevant source code.

Downgrade or discard false positives.

---

## 3. Perform repository cleanup

Beyond architectural validation, inspect the repository for the following categories.

### Dead Code

Identify:

- unused classes
- unused interfaces
- unused methods
- unused services
- unused registrations
- unused DTOs
- unused models
- unused endpoints
- unused handlers
- unused validators
- unused enums
- unused constants
- empty folders

Remove only when safe.

---

### Duplicate Code

Detect duplicated:

- business logic
- validation
- mappings
- queries
- helper methods
- utility classes

Consolidate when behavior remains unchanged.

---

### Consistency

Compare similar feature slices.

Identify inconsistencies such as:

- missing validators
- missing handlers
- inconsistent request/response models
- inconsistent endpoint structure
- inconsistent naming
- inconsistent folder organization
- inconsistent Result usage
- inconsistent error handling
- inconsistent dependency injection

Follow the project's existing conventions rather than introducing new ones.

---

### Simplification

Apply safe refactorings including:

- remove redundant conditions
- simplify LINQ
- remove unnecessary allocations
- simplify boolean expressions
- remove redundant null checks
- remove unnecessary async/await
- remove dead assignments
- simplify object initialization

Behavior must remain unchanged.

---

### Performance

Look for mechanical improvements such as:

- unnecessary LINQ allocations
- multiple enumeration
- unnecessary ToList()
- Count() where Any() is sufficient
- unnecessary async state machines
- avoidable boxing
- unnecessary string allocations

Only apply improvements with negligible behavioral risk.

---

### EF Core

Inspect for:

- missing AsNoTracking()
- unnecessary Include()
- potential N+1 queries
- duplicated queries
- orphaned migrations
- unused DbSets
- missing indexes suggested by existing query patterns

Report changes requiring schema modifications separately.

---

### Dependency Injection

Inspect for:

- duplicate registrations
- missing registrations
- incorrect lifetimes
- services registered but never resolved
- services resolved but never registered

---

### Build Quality

Identify:

- compiler warnings
- analyzer violations
- nullable reference issues
- formatting inconsistencies
- XML documentation inconsistencies (when used by the project)

Treat build failures as blockers.

---

### Security

Identify obvious issues such as:

- hardcoded secrets
- connection strings
- API keys
- credentials
- insecure configuration
- missing authorization where inconsistent with established project conventions

Report security-sensitive changes before applying them.

---

### Technical Debt

Locate and classify:

- TODO
- FIXME
- HACK
- XXX
- TEMP

Group them by severity.

---

## 4. Categorize findings

Split findings into two categories.

### Apply Automatically

Mechanical improvements that preserve behavior.

Examples:

- formatting
- naming consistency
- analyzer fixes
- dead code removal
- duplicate code extraction
- build warning fixes
- validator additions following established patterns
- documentation improvements
- consistency fixes

Apply these directly.

---

### Requires User Approval

Examples:

- API contract changes
- authorization changes
- database schema changes
- architectural redesign
- dependency replacement
- business rule changes
- feature removal
- public interface changes
- permission model changes
- breaking refactors

Present concrete options and wait for confirmation.

---

## 5. Apply fixes

Apply mechanical improvements incrementally.

After each logical group:

- Build the solution.
- Resolve any issues introduced.
- Continue only after a successful build.

Do not accumulate a large number of unchecked changes.

---

## 6. Improve Tests

Identify missing or weak test coverage.

Where appropriate:

- add unit tests
- add integration tests
- add validator tests
- add architecture tests
- improve assertions
- remove duplicated tests

Never weaken a test merely to make it pass.

If a failing test reveals a production bug, fix the production code instead.

---

## 7. Validate

Run the repository validation process.

Automatically detect the appropriate commands.

Typically this includes:

- dotnet build
- dotnet test

If multiple solutions exist, explain which one was selected and why.

Do not report completion until validation succeeds.

---

# Cleanup Principles

Always:

- Preserve behavior.
- Follow repository conventions.
- Prefer consistency over personal preference.
- Keep changes mechanical.
- Avoid speculative refactoring.
- Verify every finding before modifying code.
- Explain any decision that requires architectural judgment.

---

# Output

## Blockers

Issues preventing successful build, test execution, or correct behavior.

---

## Convention Violations

Repository inconsistencies and architectural deviations.

---

## Code Quality Improvements

Mechanical improvements applied or recommended.

---

## Test Gaps

Missing or weak automated test coverage.

---

## Changes Applied

Summarize every automatic fix.

---

## Pending Decisions

List every finding requiring user approval, including recommended options and expected impact.

---

## Validation Results

Report:

- Build
- Tests
- Analyzer results
- Remaining warnings
- Remaining TODO/FIXME items