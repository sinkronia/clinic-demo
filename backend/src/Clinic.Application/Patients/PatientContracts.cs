using Clinic.Domain.Patients;

namespace Clinic.Application.Patients;

public sealed record CreatePatientCommand(string? FullName, string? DocumentNumber, string? Phone, DateOnly BirthDate);

public sealed record PatientResponse(
    Guid Id,
    string FullName,
    string DocumentNumber,
    string Phone,
    DateOnly BirthDate,
    DateTimeOffset CreatedAt)
{
    public static PatientResponse From(Patient patient) => new(
        patient.Id,
        patient.FullName,
        patient.DocumentNumber,
        patient.Phone,
        patient.BirthDate,
        patient.CreatedAt);
}

public interface IPatientRepository
{
    /// <summary>Adds the patient. Returns false when the document number already exists in the tenant.</summary>
    Task<bool> TryAddAsync(Patient patient, CancellationToken ct);

    /// <summary>Non-archived patients whose name, document or phone contains <paramref name="search"/>.</summary>
    Task<IReadOnlyList<Patient>> SearchAsync(string? search, CancellationToken ct);
}
