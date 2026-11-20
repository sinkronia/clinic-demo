using Clinic.Application.Appointments;
using Clinic.Application.Idempotency;
using Clinic.Application.Patients;
using Microsoft.Extensions.DependencyInjection;

namespace Clinic.Application;

public static class DependencyInjection
{
    /// <summary>Registers handlers (plain classes, no mediator library).</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IdempotencyService>();
        services.AddScoped<CreatePatientHandler>();
        services.AddScoped<SearchPatientsHandler>();
        services.AddScoped<CreateAppointmentHandler>();
        services.AddScoped<CancelAppointmentHandler>();
        services.AddScoped<ListAppointmentsHandler>();
        return services;
    }
}
