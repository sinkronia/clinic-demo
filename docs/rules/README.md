# Rules

One file per rule, in `backend/` (`BE-*`) or `general/` (`GEN-*`).

## File format

```markdown
---
id: BE-006                      # unique, matches the file name
title: Short imperative title
applies_to: ["**/*.cs"]         # globs matched against changed paths (see scripts/rules-for-diff.ps1)
verified_by: ["BannedSymbols.txt", "Clinic.ArchitectureTests"]
status: active                  # proposed | active | deprecated
since: v0.1
---
**Rule.** ...
**Why.** ...
**Correct.** ...
**Incorrect.** ...
**Exception.** ... (optional)
```

### `verified_by` entries

| Form | Meaning | Checked by `validate-rules.ps1` |
| --- | --- | --- |
| `BannedSymbols.txt`, `Clinic.ArchitectureTests` | A file or folder that must exist in the project | exists |
| `package:<Name>` | A NuGet package referenced in `Directory.Build.props` | mentioned |
| `ci:<what>` | Checked by CI (tool or script) | not checked |
| `review:only` | Only human or AI review | not checked |

## How to propose a rule

1. Open a PR in this repo (or use the `propose-rule` skill from a project) adding a file with `status: proposed`.
2. Say in the PR what problem it prevents and how it will be verified automatically (prefer a build error or a test over review).
3. A rule becomes `active` when the team approves it and a new tag is cut. Projects pick it up by bumping `.code-standards-version` and running `scripts/sync-standards.ps1`.
