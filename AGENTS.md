# AGENTS.md

## Project
Dental clinic SaaS demo. Monorepo: backend/ (.NET 10, EF Core 10, PostgreSQL 18), docs/.

## Conventions
- Everything technical is in English: code, identifiers, files, folders, branches, commits.
- Domain terms come from docs/glossary.md. If a term is missing, propose it
  (skill propose-rule) before using it.

## Before writing code
1. Read the issue and the linked spec in docs/modules/.
2. Check the rules that apply: `pwsh ./scripts/rules-for-diff.ps1 <paths>` or docs/rules/.
3. If a skill exists for the task (.agents/skills/), follow it.
4. Mirror the reference code: the Patients feature in every layer.

## Commands
- `pwsh ./scripts/check.ps1` runs everything CI runs (build, format, tests).
- All scripts are PowerShell 7 (`pwsh`); they work on Windows, macOS and Linux.
- Dev database: `docker compose up -d`.

## Boundaries
- Do not edit docs/rules/ or .agents/skills/ (synced from code-standards).
- Never use real patient data in prompts, tests or seeds.
- If a rule blocks the task, do not work around it: explain it in the PR and use propose-rule.

## Done
`pwsh ./scripts/check.ps1` is green, the PR template is complete, Conventional Commits.
