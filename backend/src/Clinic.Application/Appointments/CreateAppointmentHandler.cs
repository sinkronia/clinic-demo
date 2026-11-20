using Clinic.Application.Patients;
using Clinic.Application.Results;
using Clinic.Domain.Appointments;

namespace Clinic.Application.Appointments;

public sealed class CreateAppointmentHandler(
    IAppointmentRepository appointments,
    IPatientRepository patients,
    TimeProvider time)
{
    public async Task<Result<AppointmentResponse>> HandleAsync(CreateAppointmentCommand command, CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var errors = CreateAppointmentValidator.Validate(command);
        if (errors.Count > 0)
        {
            return Result.Failure<AppointmentResponse>(new BusinessError("validation.failed", ErrorKind.Validation, errors));
        }

        if (command.StartsAt < now)
        {
            return Result.Failure<AppointmentResponse>(new BusinessError("appointment.in_past", ErrorKind.Validation));
        }

        if (!await patients.ExistsActiveAsync(command.PatientId, ct))
        {
            return Result.Failure<AppointmentResponse>(new BusinessError("patient.not_found", ErrorKind.NotFound));
        }

        // DEMO ONLY: check-then-insert is not serialized. A production system needs a database
        // exclusion constraint or a per-chair lock so two concurrent requests cannot both pass.
        if (await appointments.HasOverlapAsync(command.DentalChairId, command.StartsAt, command.EndsAt, ct))
        {
            return Result.Failure<AppointmentResponse>(new BusinessError("appointment.overlap", ErrorKind.Conflict));
        }

        var appointment = new Appointment
        {
            Id = Guid.CreateVersion7(),
            PatientId = command.PatientId,
            DentalChairId = command.DentalChairId,
            StartsAt = command.StartsAt,
            EndsAt = command.EndsAt,
            Status = AppointmentStatus.Scheduled,
            CreatedAt = now,
        };

        await appointments.AddAsync(appointment, ct);

        return Result.Success(AppointmentResponse.From(appointment));
    }
}
