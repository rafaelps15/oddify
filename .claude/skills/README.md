# .NET Modular Monolith Agent Skills for Claude Code

A skill pack that teaches Claude Code the conventions of this repository's .NET Modular Monolith
template — so every module, entity, and feature it builds looks like it was written by hand: strict
layer boundaries, MediatR-based CQRS, Result-based error handling, Dapper for reads / EF Core for
writes, and full test coverage. The authoritative description of the architecture itself lives in the
root [`CLAUDE.md`](../../CLAUDE.md); these skills are its executable counterpart.

## What's inside

| Skill | Invoke with | What it does |
|---|---|---|
| **add-entity** | `/add-entity TodoList with a name and owner` | Adds a Domain entity end to end: entity class, error catalog, domain events, repository interface, optional EF configuration, and DI/DbContext wiring. |
| **add-feature** | `/add-feature complete a todo item` | Scaffolds a complete vertical slice: command/query, handler, validator, and minimal-API endpoint. |
| **add-messaging** | `/add-messaging todo item completed, notify Rewards` | Wires a cross-module integration event end to end: contract, outbox publish, inbox consume, and (if needed) a module's first-time outbox/inbox setup. |
| **add-tests** | `/add-tests CompleteTodoItemCommand` | Writes Domain entity tests, Application handler tests (hand-written fakes, no mocking framework), and reflection-based architecture tests. |
| **ca-review** | `/ca-review` | Reviews pending changes against the template's conventions: layer boundaries, Result usage, CQRS shape, validation placement, and module isolation. |

You don't have to invoke them explicitly — once installed, Claude Code picks the right skill
automatically when you say things like "add an endpoint to archive a bet slip."

## How they fit together

`add-entity` and `add-feature` both start with a **Step 0** that resolves the repo's real
`<RootNamespace>` and `<Module>` from the code itself rather than hard-coding one — these skills are
meant to keep working as the codebase (and CLAUDE.md) evolve, not just at the moment they were written.
`add-feature` assumes the target entity already exists (created via `add-entity` if not), and its
detailed templates live under `add-feature/references/` — one file per concern (command slice, query
slice, endpoint, tests) — so the top-level `SKILL.md` stays a short workflow, not a wall of code.
`add-tests` and `ca-review` close the loop: the first backfills the full test taxonomy (including
architecture tests) for whatever the other two just scaffolded, the second checks the result against
the same conventions before it's considered done.

## Installation

The skills live in `.claude/skills/`. If you're working in this repo, they're already active — just
open it in Claude Code.

To use them in another project based on this template, copy the folder:

```
your-project/
└── .claude/
    └── skills/
        ├── README.md
        ├── add-entity/
        ├── add-feature/
        │   └── references/
        ├── add-messaging/
        ├── add-tests/
        └── ca-review/
```

`add-messaging` assumes the same hand-rolled Outbox/Inbox/`InMemoryEventBus` mechanism documented
in `CLAUDE.md` §5/§10 — if the target project uses a real message broker instead, that skill (and
those sections) need adapting first, the other four don't depend on it either way.

## Try it

```
/add-feature snooze a todo item until a given date
```

Claude will create the command, validator, handler (delegating the rule to the entity, raising a
domain event, saving via `IUnitOfWork`), and the endpoint — then you can follow up with `/add-tests` and
`/ca-review` to backfill coverage and confirm it matches the template before committing.

## Customizing

Each skill is a plain Markdown file (`SKILL.md`, plus templates under `add-feature/references/`).
Renamed a layer, prefer a different test stack, added a new module convention? Update `CLAUDE.md` first
— it's the source of truth this pack is meant to track — then edit the matching skill so future
scaffolding follows suit.
