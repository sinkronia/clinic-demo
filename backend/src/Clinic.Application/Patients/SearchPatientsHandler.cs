namespace Clinic.Application.Patients;

public sealed class SearchPatientsHandler(IPatientRepository patients)
{
    public async Task<IReadOnlyList<PatientResponse>> HandleAsync(string? search, CancellationToken ct)
    {
        var found = await patients.SearchAsync(search?.Trim(), ct);
        return [.. found.Select(PatientResponse.From)];
    }
}
