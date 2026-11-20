using Clinic.Application.Appointments;
using Clinic.Domain.Appointments;
using Clinic.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Clinic.Infrastructure.Persistence;

internal sealed class AppointmentRepository(ClinicDbContext db) : IAppointmentRepository
{
    public async Task<bool> TryAddAsync(Appointment appointment, CancellationToken ct)
    {
        db.Appointments.Add(appointment);
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation, ConstraintName: AppointmentConfiguration.NoOverlapConstraint })
        {
            // The database constraint is the source of truth, so two concurrent requests cannot both win.
            db.Entry(appointment).State = EntityState.Detached;
            return false;
        }
    }

    public Task<Appointment?> FindAsync(Guid id, CancellationToken ct) =>
        db.Appointments.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public async Task<IReadOnlyList<Appointment>> ListStartingBetweenAsync(DateTimeOffset fromInclusive, DateTimeOffset toExclusive, CancellationToken ct) =>
        await db.Appointments.AsNoTracking()
            .Where(a => a.StartsAt >= fromInclusive && a.StartsAt < toExclusive)
            .OrderBy(a => a.StartsAt).ThenBy(a => a.Id)
            .ToListAsync(ct);
}
