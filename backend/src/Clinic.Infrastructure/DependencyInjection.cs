using Clinic.Application.Appointments;
using Clinic.Application.Idempotency;
using Clinic.Application.Patients;
using Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clinic.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ClinicDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Clinic")
                ?? throw new InvalidOperationException("Connection string 'Clinic' is not configured.")));
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        return services;
    }

    /// <summary>Applies pending migrations. Demo only: production would run migrations as a deployment step.</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
        await db.Database.MigrateAsync(ct);
    }
}
