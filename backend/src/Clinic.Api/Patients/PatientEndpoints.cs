using Clinic.Api.Http;
using Clinic.Application.Idempotency;
using Clinic.Application.Patients;

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

        return app;
    }
}
