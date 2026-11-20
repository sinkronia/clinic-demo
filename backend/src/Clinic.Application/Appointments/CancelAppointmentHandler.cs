using Clinic.Application.Results;
using Clinic.Domain.Appointments;

namespace Clinic.Application.Appointments;

public sealed class CancelAppointmentHandler(IAppointmentRepository appointments)
{
    public async Task<Result<AppointmentResponse>> HandleAsync(Guid id, CancellationToken ct)
    {
        var appointment = await appointments.FindAsync(id, ct);
        if (appointment is null)
        {
            return Result.Failure<AppointmentResponse>(new BusinessError("appointment.not_found", ErrorKind.NotFound));
        }

        if (appointment.Status != AppointmentStatus.Cancelled)
        {
            appointment.Status = AppointmentStatus.Cancelled;
            await appointments.SaveChangesAsync(ct);
        }

        return Result.Success(AppointmentResponse.From(appointment));
    }
}
