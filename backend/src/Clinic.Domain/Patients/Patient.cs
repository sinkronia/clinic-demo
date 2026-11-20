namespace Clinic.Domain.Patients;

public sealed class Patient : ITenantEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public required string FullName { get; set; }

    /// <summary>Bolivian CI (national identity document number). Unique per tenant.</summary>
    public required string DocumentNumber { get; set; }

    public required string Phone { get; set; }

    public DateOnly BirthDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Patients are archived, never deleted.</summary>
    public DateTimeOffset? ArchivedAt { get; set; }
}
