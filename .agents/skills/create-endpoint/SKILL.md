---
name: create-endpoint
description: Add an HTTP endpoint (minimal API) with its Application-layer handler, validation, ProblemDetails errors and idempotency. Use for any new route.
---

# Create an endpoint

Rules involved: BE-004, BE-007, BE-009, BE-010, GEN-004.

1. Read the spec in `docs/modules/` and the issue. Check terms against `docs/glossary.md`.
2. Application (`backend/src/Clinic.Application/`): write a command or query record plus a handler class (`<Name>Handler`) registered in DI. The handler holds all business rules, uses `TimeProvider` for time, and returns a result type; it throws no exceptions for business errors.
3. Business errors map to `ProblemDetails` with a stable `code` (for example `appointment.overlap`). Validation errors are `400` with field errors and codes. Never return free text meant for users.
4. Api (`backend/src/Clinic.Api/`): map the route in a `Map<Feature>Endpoints` extension. The endpoint only converts HTTP to the command/query and the result to a response. It must not reference `DbContext`.
5. Create endpoints (`POST`) accept `Idempotency-Key` and use the existing idempotency mechanism (`IdempotencyRecords`).
6. Every route requires the tenant (`X-Tenant-Id`); a missing header gives `400` `tenant.missing`.
7. Tests: unit tests for validation, integration tests (Testcontainers) for success, each business error code, tenant isolation, and idempotent replay.
8. Mirror the Patients endpoints in every layer.
9. Run `pwsh ./scripts/check.ps1` and fix everything before opening the PR.
