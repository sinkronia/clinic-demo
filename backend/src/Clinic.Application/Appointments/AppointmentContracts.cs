using Clinic.Domain.Appointments;

namespace Clinic.Application.Appointments;

public sealed record CreateAppointmentCommand(
    Guid PatientId,
    Guid DentalChairId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt);

public sealed record AppointmentResponse(
    Guid Id,
    Guid PatientId,
    Guid DentalChairId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string Status,
    DateTimeOffset CreatedAt)
{
    public static AppointmentResponse From(Appointment appointment) => new(
        appointment.Id,
        appointment.PatientId,
        appointment.DentalChairId,
        appointment.StartsAt,
        appointment.EndsAt,
        appointment.Status.ToString(),
        appointment.CreatedAt);
}

public interface IAppointmentRepository
{
    /// <summary>True when a non-cancelled appointment on the chair intersects the given range. Touching boundaries do not overlap.</summary>
    Task<bool> HasOverlapAsync(Guid dentalChairId, DateTimeOffset startsAt, DateTimeOffset endsAt, CancellationToken ct);

    Task<Appointment?> FindAsync(Guid id, CancellationToken ct);

    Task AddAsync(Appointment appointment, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);

    /// <summary>Appointments starting on <paramref name="day"/> (UTC), ordered by start.</summary>
    Task<IReadOnlyList<Appointment>> ListByDateAsync(DateOnly day, CancellationToken ct);
}
