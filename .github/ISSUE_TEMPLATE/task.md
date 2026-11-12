---
name: Task
about: An independently mergeable change. Scope, boundaries, and acceptance criteria must be clear.
title: ''
labels: 'type: task'
assignees: ''
---

<!--
Suggested title format: [Module] concise action.
-->

## Background and goal

Describe the symptom, trigger conditions, and impact. Include reproduction steps, failing output, or affected providers when available.

Related code/docs: `src/...`

## Fixed contracts and invariants

- State clearly what must stay fixed among the public API, SQL, transactions, pagination, provider dialects, target frameworks, or package metadata.
- If a fix has to cover multiple paths, state the shared invariants and the boundary where the paths converge and the fix should land.

## Scope

- What will change.
- **Explicitly excluded**: especially limits such as "do not change existing runtime behavior", "do not refactor X", "no new provider".

## Exclusions and non-guarantees

Security or robustness tasks must state separately what is guaranteed, what is not guaranteed, and caller responsibilities; other tasks write "None".

- Guarantees:
- Not guaranteed:
- Caller responsibilities:

## Acceptance criteria

- Individually verifiable successful outcomes.
- Applicable failure, cancellation, security, and concurrency outcomes; when not applicable, state "not applicable" and why.
- When the public API, providers, transactions, package boundaries, or releases are involved, state the compatibility outcome.

## Documentation and compatibility impact

State whether the public API, SQL/providers, target frameworks, package versions, transaction semantics, release process, or user-facing documentation are affected; write "None" when there is no impact.

## Known adjacent issues (not fixed this time)

- #NNN one-line summary
- If confirmed none, write "None".

## Dependencies

- GitHub-native Blocked by: #NNN, or "None"
- GitHub-native Blocks: #NNN, or "None"

## Delivery constraints

- When public behavior or usage changes, update the English README, XML docs, and release notes in sync.
