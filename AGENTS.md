# CoddLoom Collaboration Guidelines

A lightweight, explicit ORM built on ADO.NET: a `netstandard2.0` core package plus a separate provider package for each database.
There is no LINQ provider, no change tracker, and no implicit unit of work.

## Maintenance

- This file is the single source of truth for in-repo AI collaboration process and constraints.
- Codex reads this file directly; Claude Code v2.1.277+ likewise reads this file directly, without a `CLAUDE.md` import.
- There is no separate `CONTRIBUTING.md` in this repository; this file is the unified entry point for AI and contributor workflow, review policy, and project boundaries.
- Change only this file when modifying general rules. Unless a rule really applies to a single tool only, do not put rule text into
  `CLAUDE.md` or other tool entry files, to avoid drift or conflicts.

## Documentation language

- PR titles keep the English conventional commit format (`feat:` / `fix:` / `docs:` …), because after a squash merge the PR title becomes the commit title on `main`.
- **User-facing documentation is written in English**: `README.md`, `THIRD-PARTY-NOTICES.md`, XML documentation comments in code,
  exception messages, and caller-facing text in attributes such as `[Obsolete]`. These are part of the public release.
- Code identifiers and commit messages stay in English.

## Project boundaries and contracts

- `src/CoddLoom` is the database-independent core; database drivers, dialects, and provider-specific behavior may live only in the
  corresponding `src/CoddLoom.*` provider packages.
- The public API, SQL generation, parameterization behavior, transaction ownership, pagination semantics, and published packages/target
  frameworks are all compatibility contracts. Do not introduce a LINQ provider, implicit change tracker, or implicit commit for caller
  convenience.
- The SQL contracts of all six providers must remain independently verifiable; real-database integration tests run only when a change
  touches the corresponding provider, SQL, mapping, transactions, or concurrency semantics.

## Verification

```bash
dotnet restore CoddLoom.sln
dotnet build CoddLoom.sln --configuration Release --no-restore
dotnet test CoddLoom.sln --configuration Release --no-build --no-restore
```

When provider SQL, database connections, transactions, mapping, or concurrency are involved, run the corresponding contract/database
integration tests; Oracle real-database tests stay explicitly opt-in. When package metadata or the release path is involved, verify the
six providers' local packaging results, target-framework compatibility, and symbol packages; do not replace release verification with a
single ordinary unit test run.

## Security and change discipline

- Never commit connection strings, passwords, tokens, private keys, test database files, or private data.
- Before committing, check documentation links and formatting.

## Known debt hotspots

None currently. The former `Generate*Id` hotspot in `src/CoddLoom/DbEngine.Extension.cs` was
removed with the deprecated API family in 0.3.0 (issues #36-#39 remain closed as history).

## English working environment

Write in English: project-maintained documentation (README and docs), Issue and Change Request titles and bodies, Comments, Discussions, Review Threads, and future commit messages, unless a maintainer explicitly requests another language. Preserve historical Issues, Change Requests, and comments. Do not translate code, commands, paths, identifiers, logs, technical data, or quoted external text. Preserve other project conventions.
