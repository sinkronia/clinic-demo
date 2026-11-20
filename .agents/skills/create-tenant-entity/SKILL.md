---
name: create-tenant-entity
description: Add a new business entity (Domain + EF mapping + migration + tests) that is correctly multi-tenant. Use whenever a new table/entity is needed.
---

# Create a tenant entity

Rules involved: BE-003, BE-005, BE-006, BE-007, GEN-004.

1. Check the name against `docs/glossary.md`. If the term is missing, stop and use the `propose-rule` skill first.
2. Domain (`backend/src/Clinic.Domain/`): create a `sealed` class implementing `ITenantEntity`. No EF attributes, no ASP.NET types. Ids are `Guid`; timestamps are `DateTimeOffset` set from `TimeProvider`, never from the system clock.
3. Infrastructure (`backend/src/Clinic.Infrastructure/`): add the `DbSet<T>` and an `IEntityTypeConfiguration<T>` (table name, keys, max lengths, indexes). Add a unique index that includes `TenantId` when uniqueness is per tenant. Do not write `Where(x => x.TenantId == ...)`: the global query filter in the DbContext already covers every `ITenantEntity`.
4. Migration: `dotnet ef migrations add <Name> -p backend/src/Clinic.Infrastructure -s backend/src/Clinic.Api`. Never edit an existing merged migration.
5. Tests: the architecture test for BE-005 picks the entity up automatically; add an integration test proving tenant B cannot see tenant A's rows.
6. Mirror the `Patient` entity and its configuration.
7. Run `pwsh ./scripts/check.ps1` and fix everything before opening the PR.
