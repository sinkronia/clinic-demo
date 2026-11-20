using Clinic.Application.Patients;
using Clinic.Application.Results;
using Clinic.Domain.Appointments;

namespace Clinic.Application.Appointments;

public sealed class CreateAppointmentHandler(IAppointmentRepository appointments, IPatientRepository patients, TimeProvider time)
{
    public async Task<Result<AppointmentResponse>> HandleAsync(CreateAppointmentCommand command, CancellationToken ct)
    {
        var errors = CreateAppointmentValidator.Validate(command);
        if (errors.Count > 0)
        {
            return Result.Failure<AppointmentResponse>(new BusinessError("validation.failed", ErrorKind.Validation, errors));
        }

        var now = time.GetUtcNow();
        if (command.StartsAt < now)
        {
            return Result.Failure<AppointmentResponse>(new BusinessError("appointment.in_past", ErrorKind.Validation));
        }

        if (!await patients.ExistsActiveAsync(command.PatientId!.Value, ct))
        {
            return Result.Failure<AppointmentResponse>(new BusinessError("patient.not_found", ErrorKind.NotFound));
        }

        var appointment = new Appointment
        {
            Id = Guid.CreateVersion7(),
            PatientId = command.PatientId.Value,
            DentalChairId = command.DentalChairId!.Value,
            StartsAt = command.StartsAt.ToUniversalTime(),
            EndsAt = command.EndsAt.ToUniversalTime(),
            Status = AppointmentStatus.Scheduled,
            CreatedAt = now,
        };

        if (!await appointments.TryAddAsync(appointment, ct))
        {
            return Result.Failure<AppointmentResponse>(new BusinessError("appointment.overlap", ErrorKind.Conflict));
        }

        return Result.Success(AppointmentResponse.From(appointment));
    }
}
