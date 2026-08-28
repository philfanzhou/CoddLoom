# CoddLoom

A lightweight, explicit ORM over ADO.NET: `netstandard2.0` core plus per-provider
packages. No LINQ provider, no change tracker, no hidden unit of work.

## Scope discipline

This repository has a recurring failure mode: a narrow, well-specified issue turns
into a long PR review cycle that will not converge. The mechanism is always the same
one — a review finds a real pre-existing bug in code sitting next to the diff, the bug
gets fixed inside the same PR, that fix creates fresh reviewable surface (behavior
change, compatibility note, docs, release note), and the cycle repeats.

See [PR #34](https://github.com/philfanzhou/CoddLoom/pull/34) for the worst instance:
issue #24 explicitly ruled out any runtime behavior change, yet three of its seven
review findings were pre-existing defects in `DbEngine.Extension.cs`. One was fixed
in-PR anyway (`068da65b`, culture-invariant time IDs), and that single out-of-scope
commit produced a seventh finding of its own — persisted-ID compatibility, README,
release note — i.e. another full round. Contrast
[PR #33](https://github.com/philfanzhou/CoddLoom/pull/33), where the equivalent
pre-existing finding was deferred to issue #40 and the PR merged instead of absorbing
it.

The rules below exist to break that loop. They are not style preferences.

### Before declaring an issue ready to implement

Never judge readiness from the issue description alone. Open every file and member the
issue names and read them end to end first.

Any defect you find in that code which the issue does not ask for is **adjacent debt**.
For each one: file it as its own issue, then link it from the target issue under a
`## 已知邻近问题（本次不修）` section.

An issue is ready only when all three hold:

1. It states its own `## 最小修改范围` and `## 验收标准`.
2. The code it will touch has been read.
3. Adjacent debt in that code is already filed and linked.

"The description is clear" is not readiness. A clear issue over rotten neighboring code
is exactly the case that fails.

### While implementing

The issue's `最小修改范围` is binding. Do not change runtime behavior the issue said
would not change — not even to fix something that is genuinely, verifiably broken.
File it instead.

### While reviewing

Classify every finding *before* writing it up:

- **Introduced by this PR** — the defect is in a line this PR adds or modifies.
  Fix it in this PR.
- **Pre-existing** — the defect is in code this PR only moves, reindents, renames
  around, or merely sits next to. File an issue, link it from the review comment, and
  say explicitly that it is out of scope here. Do not fix it in this PR.

One exception: a pre-existing defect that makes this PR's own `验收标准` impossible to
verify. Name the acceptance criterion you are invoking when you use it.

A finding being real, reproducible and well-evidenced does not make it in scope. Scope
is decided by which lines the PR touches, not by how good the finding is.

### Tripwire

At the third review round on a PR, stop before writing more code and diff its commits
against the issue's `最小修改范围`. Any commit not traceable to a listed acceptance
criterion is a scope violation: drop it and file an issue instead.

## Known debt hotspot

`src/CoddLoom/DbEngine.Extension.cs` — the client-side `Generate*Id` family produced
issues #36, #37, #38 and #39 in a single review pass. Apply the adjacent-debt inventory
with extra care before scheduling any work that touches it.
