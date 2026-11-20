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
                ToHttp(await handler.HandleAsync(id, ct)));

        group.MapGet(
            "/",
            async (string? date, ListAppointmentsHandler handler, CancellationToken ct) =>
                ToHttp(await handler.HandleAsync(date, ct)));

        return app;
    }

    private static IResult ToHttp<T>(Result<T> result) =>
        result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Json(Problems.From(result.Error!), statusCode: Problems.StatusFor(result.Error!.Kind), contentType: "application/problem+json");
}
