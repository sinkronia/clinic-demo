using Clinic.Domain.Appointments;

namespace Clinic.Application.Appointments;

public sealed record CreateAppointmentCommand(Guid? PatientId, Guid? DentalChairId, DateTimeOffset StartsAt, DateTimeOffset EndsAt);

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
    /// <summary>Adds the appointment. Returns false when it overlaps a scheduled appointment on the same chair.</summary>
    Task<bool> TryAddAsync(Appointment appointment, CancellationToken ct);

    /// <summary>The appointment with this id in the current tenant, tracked for update; null when unknown.</summary>
    Task<Appointment?> FindAsync(Guid id, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);

    /// <summary>Appointments starting in [<paramref name="fromInclusive"/>, <paramref name="toExclusive"/>), ordered by start.</summary>
    Task<IReadOnlyList<Appointment>> ListStartingBetweenAsync(DateTimeOffset fromInclusive, DateTimeOffset toExclusive, CancellationToken ct);
}
