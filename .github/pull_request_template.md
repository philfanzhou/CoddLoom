> Use the English conventional commit format for the title (`feat:` / `fix:` / `docs:` …).

## Overview

Explain the problem being solved and what this change does.

Closes #

## Scope

Quote the linked issue's "Scope" and account for each item:

- **In scope, completed:**
- **Deliberately not fixed in this PR:** write "None" when confirmed none.

## Contracts and compatibility

- **Guarantees and non-guarantees:** consistent with the issue, or "None" when not applicable.
- **Public API / SQL / behavior:**
- **Providers, target frameworks, and packages:**
- **Transactions, concurrency, and databases:**
- **User documentation and releases:**

Write "None" for unaffected items.

## Verification

List the commands actually run, their results, and anything skipped with reasons:

- [ ] Release build passes
- [ ] Unit/contract tests pass
- [ ] Applicable provider database integration tests pass, or explicitly waiting on CI
- [ ] When packages or the release path are involved, packaging/compatibility checks for the six providers pass
- [ ] When behavior, public API, or usage changes, English user-facing documentation is updated
- [ ] Contains no secrets, connection strings, credentials, or private data
