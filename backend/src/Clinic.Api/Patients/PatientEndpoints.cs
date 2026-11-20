using Clinic.Api.Http;
using Clinic.Application.Idempotency;
using Clinic.Application.Patients;
using Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Api.Patients;

internal static class PatientEndpoints
{
    public static IEndpointRouteBuilder MapPatientEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/patients");

        group.MapPost(
            "/",
            (HttpContext http, CreatePatientCommand command, CreatePatientHandler handler, IdempotencyService idempotency) =>
                IdempotentCreate.RunAsync(http, idempotency, command, ct => handler.HandleAsync(command, ct)));

        group.MapGet(
            "/",
            async (string? search, SearchPatientsHandler handler, CancellationToken ct) =>
                Results.Ok(await handler.HandleAsync(search, ct)));

        // DEMO: intentionally breaks BE-004 (DbContext in an endpoint), BE-006 (IgnoreQueryFilters), BE-007 (DateTime.UtcNow).
        group.MapGet(
            "/count",
            async (ClinicDbContext db, CancellationToken ct) =>
                Results.Ok(new
                {
                    count = await db.Patients.IgnoreQueryFilters().CountAsync(ct),
                    at = DateTime.UtcNow,
                }));

        return app;
    }
}
