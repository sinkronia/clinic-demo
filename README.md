# clinic-demo

Demo project for Sinkronia: a small multi-tenant dental clinic backend (.NET 10, ASP.NET Core minimal APIs, EF Core 10 + Npgsql, PostgreSQL 18). Its purpose is to show that code written by different AI tools and models converges to the same standards, because every change goes through the same shared context ([code-standards](https://github.com/Sinkronia/code-standards): rules, skills, glossary) and the same automated gates (CI, AI reviewer, human approval).

No real patient data anywhere. Both this repo and `code-standards` are **public for the demo** and become private later.

## Run it

```powershell
docker compose up -d                       # dev PostgreSQL 18
dotnet run --project backend/src/Clinic.Api
pwsh ./scripts/check.ps1                   # everything CI runs: build, format, tests (needs Docker)
```

Every request needs the `X-Tenant-Id: <guid>` header. `POST /patients` also needs `Idempotency-Key`.

> **Demo-only tenant resolution.** The tenant comes from a client-supplied header. A real deployment will read a tenant claim from the user's JWT. This is also noted in `TenantMiddleware`.

## Layout

- `backend/` .NET solution (Domain, Application, Infrastructure, Api + unit, integration and architecture tests). The reference slice is **Patients**: agents imitate it.
- `docs/glossary.md` domain terms; `docs/modules/` specs; `docs/rules/` and `.agents/skills/` are **synced** from code-standards (do not edit).
- `AGENTS.md` instructions for any AI agent (`CLAUDE.md` just imports it).
- `.code-standards-version` pins the standards version. Bump it with `pwsh ./scripts/sync-standards.ps1 -Tag <tag>`.

## CI and merge protection

`.github/workflows/ci.yml` calls the reusable workflows of code-standards: `dotnet` (build with warnings as errors, format, tests), `security` (gitleaks), `rules` (synced files match the pinned version, rules valid), and `ai-review` (runs only when the PR has the `ai-review` label; needs the repo secret `CLAUDE_CODE_OAUTH_TOKEN`). `ci-ok` aggregates the required jobs.

Branch protection on `main`: PR required, 1 approval, approval of the most recent push by someone other than the pusher, code owner review, required check `ci-ok`, no force-push. **`enforce_admins` is off** so the owner can bypass and merge during the single-account demo. Team rule from now on: nobody approves their own PR, nor code from an agent they launched.

`CODEOWNERS` lists the owner's user because the organization has no `core` team yet.

## Demo limitations and TODOs

- Idempotency: the response is stored after the operation, not in the same transaction, and concurrent requests with the same key are not serialized.
- Migrations are applied at API startup (demo convenience).
- BE-011 (immutable migrations) and GEN-001 (commitlint) are `proposed`, not yet automated.
- No authentication, no frontend.
