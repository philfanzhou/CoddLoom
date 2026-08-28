## Summary

Describe the problem and the change.

Closes #

## Scope

Quote the `最小修改范围` from the linked issue, then account for it:

- **In scope, done:**
- **Deliberately not fixed here** (pre-existing defects found while working; each one
  filed as its own issue): #NNN — one line. Write `None` if there were none.

Nothing in this PR should change runtime behavior the issue ruled out. If it does, say
which acceptance criterion required it.

## Compatibility

List public API, database, SQL generation, provider, package, configuration, or migration impact. Write `None` when there is no impact.

## Verification

- [ ] .NET solution builds
- [ ] Unit tests pass
- [ ] Relevant database integration or provider contract tests pass
- [ ] Package build and restore checks pass when packaging is affected
- [ ] Documentation is updated when behavior or usage changes
- [ ] No secrets or sensitive data are included
- [ ] Every commit is traceable to an acceptance criterion in the linked issue
