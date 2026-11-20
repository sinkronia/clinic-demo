---
name: review-pr
description: Review a pull request against the project rules and produce a structured review. Used by the AI reviewer and by humans.
---

# Review a PR

You receive: this skill, the project AGENTS.md, the rule files that apply to the changed files, the glossary, and a PR number.

## Steps

1. Read the PR diff (`gh pr diff <number>`) and the linked issue/spec.
2. For each changed file, check it against the rules provided. Judge only what automated checks cannot: the rules marked `review:only`, the glossary (GEN-004), idempotency (BE-010), tenant safety, and test quality. Do not repeat what the compiler, analyzers or CI already report.
3. Check every domain term in code, routes and docs against the glossary. An unknown term is a finding (GEN-004).
4. Check that tests exist for each business rule in the spec, including boundary cases.

## Output

Post **one PR review** with inline comments.

- Every inline comment starts with `[RULE-ID][blocker|major|minor]`, for example `[BE-010][major] ...`.
- **No comment without a rule id.** If a concern maps to no rule, do not post it as a finding; mention it in the summary under "Suggest a rule".
- Severity: `blocker` = breaks a rule that protects data or architecture; `major` = breaks a rule, should be fixed before merge; `minor` = small deviation.
- Finish the review body with a summary table:

| Rule | Severity | File:line | Finding |
| --- | --- | --- | --- |

- If there are no findings, say so in one line.
- During the demo the review only comments; it never approves or requests changes.

Finish with: run `pwsh ./scripts/check.ps1` and fix everything before opening the PR (when you are the author of changes, not when you are only reviewing).
