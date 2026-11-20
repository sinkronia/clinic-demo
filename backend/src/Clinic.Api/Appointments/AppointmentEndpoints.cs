using Clinic.Api.Http;
using Clinic.Application.Appointments;
using Clinic.Application.Idempotency;
using Clinic.Application.Results;

namespace Clinic.Api.Appointments;

internal static class AppointmentEndpoints
{
    public static IEndpointRouteBuilder MapAppointmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/appointments");

        group.MapPost(
            "/",
            (HttpContext http, CreateAppointmentCommand command, CreateAppointmentHandler handler, IdempotencyService idempotency) =>
                IdempotentCreate.RunAsync(http, idempotency, command, ct => handler.HandleAsync(command, ct)));

        group.MapPost(
            "/{id:guid}/cancel",
            async (Guid id, CancelAppointmentHandler handler, CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error!);
            });

        group.MapGet(
            "/",
            async (DateOnly? date, ListAppointmentsHandler handler, CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(date, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error!);
            });

        return app;
    }

    private static IResult Problem(BusinessError error)
    {
        var problem = Problems.From(error);
        return Results.Json(problem, statusCode: problem.Status, contentType: "application/problem+json");
    }
}
