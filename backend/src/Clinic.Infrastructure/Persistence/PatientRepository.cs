using Clinic.Application.Patients;
using Clinic.Domain.Patients;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Clinic.Infrastructure.Persistence;

internal sealed class PatientRepository(ClinicDbContext db) : IPatientRepository
{
    public async Task<bool> TryAddAsync(Patient patient, CancellationToken ct)
    {
        db.Patients.Add(patient);
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            db.Entry(patient).State = EntityState.Detached;
            return false;
        }
    }

    public Task<bool> ExistsActiveAsync(Guid id, CancellationToken ct) =>
        db.Patients.AnyAsync(p => p.Id == id && p.ArchivedAt == null, ct);

    public async Task<IReadOnlyList<Patient>> SearchAsync(string? search, CancellationToken ct)
    {
        var query = db.Patients.AsNoTracking().Where(p => p.ArchivedAt == null);
        if (!string.IsNullOrEmpty(search))
        {
            var pattern = "%" + search.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal) + "%";
            query = query.Where(p =>
                EF.Functions.ILike(p.FullName, pattern) ||
                EF.Functions.ILike(p.DocumentNumber, pattern) ||
                EF.Functions.ILike(p.Phone, pattern));
        }

        return await query.OrderBy(p => p.FullName).ThenBy(p => p.Id).ToListAsync(ct);
    }
}
