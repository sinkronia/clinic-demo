---
name: write-tests
description: Write unit, integration and architecture tests the way this project expects. Use when adding or changing behavior.
---

# Write tests

Rules involved: BE-003, BE-004, BE-005, BE-007, BE-009, BE-012.

1. Every business rule gets at least one test. Name tests `Method_Scenario_ExpectedResult`.
2. Unit tests (`Clinic.UnitTests`): pure logic and validation. Time comes from `FakeTimeProvider`.
3. Integration tests (`Clinic.IntegrationTests`): real PostgreSQL via Testcontainers (`postgres:18`), through the HTTP API with `WebApplicationFactory`. Never EF InMemory or SQLite.
4. For each endpoint cover: success, every business error `code`, missing tenant, tenant isolation (tenant B must not see tenant A's data), and idempotent replay for creates.
5. Boundary cases are tests too: for time ranges, touching boundaries (10:00-10:30 and 10:30-11:00) are not overlaps.
6. Use synthetic data only (GEN-003).
7. Architecture tests (`Clinic.ArchitectureTests`) enforce layer rules; extend them when you add a layer rule, do not weaken them.
8. Mirror the Patients tests.
9. Run `pwsh ./scripts/check.ps1` and fix everything before opening the PR.
