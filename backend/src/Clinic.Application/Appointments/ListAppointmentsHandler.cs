using System.Globalization;
using Clinic.Application.Results;

namespace Clinic.Application.Appointments;

public sealed class ListAppointmentsHandler(IAppointmentRepository appointments)
{
    /// <summary>Appointments starting on <paramref name="date"/> (yyyy-MM-dd, UTC), cancelled ones included.</summary>
    public async Task<Result<IReadOnlyList<AppointmentResponse>>> HandleAsync(string? date, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(date))
        {
            return Invalid("required");
        }

        if (!DateOnly.TryParseExact(date.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
        {
            return Invalid("invalid_format");
        }

        var from = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var found = await appointments.ListStartingBetweenAsync(from, from.AddDays(1), ct);
        return Result.Success<IReadOnlyList<AppointmentResponse>>([.. found.Select(AppointmentResponse.From)]);
    }

    private static Result<IReadOnlyList<AppointmentResponse>> Invalid(string code) =>
        Result.Failure<IReadOnlyList<AppointmentResponse>>(
            new BusinessError("validation.failed", ErrorKind.Validation, new Dictionary<string, string[]> { ["date"] = [code] }));
}
