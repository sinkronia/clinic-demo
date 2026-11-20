namespace Clinic.Domain.Appointments;

public enum AppointmentStatus
{
    Scheduled,
    Cancelled,
}

public sealed class Appointment : ITenantEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid PatientId { get; set; }

    /// <summary>The dental chair (sillón dental) the appointment uses. Overlaps are checked per chair.</summary>
    public Guid DentalChairId { get; set; }

    public DateTimeOffset StartsAt { get; set; }

    public DateTimeOffset EndsAt { get; set; }

    public AppointmentStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
