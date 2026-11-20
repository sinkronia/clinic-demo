using Clinic.Application.Appointments;
using Clinic.Domain.Appointments;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Infrastructure.Persistence;

internal sealed class AppointmentRepository(ClinicDbContext db) : IAppointmentRepository
{
    public Task<bool> HasOverlapAsync(Guid dentalChairId, DateTimeOffset startsAt, DateTimeOffset endsAt, CancellationToken ct) =>
        db.Appointments.AsNoTracking().AnyAsync(
            a => a.DentalChairId == dentalChairId &&
                a.Status != AppointmentStatus.Cancelled &&
                a.StartsAt < endsAt &&
                a.EndsAt > startsAt,
            ct);

    public Task<Appointment?> FindAsync(Guid id, CancellationToken ct) =>
        db.Appointments.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task AddAsync(Appointment appointment, CancellationToken ct)
    {
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public async Task<IReadOnlyList<Appointment>> ListByDateAsync(DateOnly day, CancellationToken ct)
    {
        var start = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var end = start.AddDays(1);

        return await db.Appointments.AsNoTracking()
            .Where(a => a.StartsAt >= start && a.StartsAt < end)
            .OrderBy(a => a.StartsAt)
            .ThenBy(a => a.Id)
            .ToListAsync(ct);
    }
}
