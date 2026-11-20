---
name: propose-rule
description: Propose a new rule or a missing glossary term instead of working around a blocker. Use when a rule gets in the way, a rule is missing, or a domain term is not in docs/glossary.md.
---

# Propose a rule or a glossary term

Rules involved: GEN-004, and any rule that blocks you.

Never edit `docs/rules/` or `.agents/skills/` in a project: they are synced from `Sinkronia/code-standards`. Never bypass a rule.

## Missing glossary term

1. Add a row to `docs/glossary.md` in the same PR: `Spanish (UI/docs) | English (code) | Definition`, marked in the PR description as a **glossary proposal**.
2. If the term is unclear (several English words fit, or the meaning is ambiguous), do not guess: state your proposal in the PR and ask for confirmation before using the term in code.

## Rule that blocks the task, or a missing rule

1. Explain in the PR which rule blocks you and why; keep the code compliant.
2. Open a PR in `Sinkronia/code-standards` adding `rules/<backend|general>/<ID>.md` with `status: proposed` (format in `rules/README.md`), or an issue there describing the change.
3. State how it would be verified automatically.

Finish by running `pwsh ./scripts/check.ps1` and fixing everything before opening the PR.
