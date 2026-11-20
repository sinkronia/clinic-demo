using System.Linq.Expressions;
using Clinic.Application.Abstractions;
using Clinic.Domain;
using Clinic.Domain.Appointments;
using Clinic.Domain.Idempotency;
using Clinic.Domain.Patients;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Infrastructure.Persistence;

public sealed class ClinicDbContext(DbContextOptions<ClinicDbContext> options, ITenantContext tenant) : DbContext(options)
{
    public DbSet<Patient> Patients => Set<Patient>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    // Read by the global query filter; EF re-evaluates it per context instance.
    private Guid CurrentTenantId => tenant.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClinicDbContext).Assembly);

        // BE-005: every ITenantEntity is filtered by the current tenant. No hand-written TenantId filters elsewhere.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(t => typeof(ITenantEntity).IsAssignableFrom(t.ClrType)))
        {
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var filter = Expression.Lambda(
                Expression.Equal(
                    Expression.Property(parameter, nameof(ITenantEntity.TenantId)),
                    Expression.Property(Expression.Constant(this), nameof(CurrentTenantId))),
                parameter);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<ITenantEntity>().Where(e => e.State == EntityState.Added))
        {
            entry.Entity.TenantId = tenant.TenantId;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
