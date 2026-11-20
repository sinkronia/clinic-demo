using Clinic.Application.Results;

namespace Clinic.Application.Appointments;

public sealed class ListAppointmentsHandler(IAppointmentRepository appointments)
{
    public async Task<Result<IReadOnlyList<AppointmentResponse>>> HandleAsync(DateOnly? date, CancellationToken ct)
    {
        if (date is null)
        {
            var errors = new Dictionary<string, string[]> { ["date"] = ["required"] };
            return Result.Failure<IReadOnlyList<AppointmentResponse>>(
                new BusinessError("validation.failed", ErrorKind.Validation, errors));
        }

        var found = await appointments.ListByDateAsync(date.Value, ct);
        return Result.Success<IReadOnlyList<AppointmentResponse>>([.. found.Select(AppointmentResponse.From)]);
    }
}
