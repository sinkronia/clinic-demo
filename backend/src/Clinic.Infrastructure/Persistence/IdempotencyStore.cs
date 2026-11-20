using Clinic.Application.Idempotency;
using Clinic.Domain.Idempotency;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Infrastructure.Persistence;

internal sealed class IdempotencyStore(ClinicDbContext db) : IIdempotencyStore
{
    public Task<IdempotencyRecord?> FindAsync(string key, CancellationToken ct) =>
        db.IdempotencyRecords.AsNoTracking().FirstOrDefaultAsync(r => r.Key == key, ct);

    public async Task SaveAsync(IdempotencyRecord record, CancellationToken ct)
    {
        db.IdempotencyRecords.Add(record);
        await db.SaveChangesAsync(ct);
    }
}
