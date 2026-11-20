using Clinic.Application.Results;
using Clinic.Domain.Patients;

namespace Clinic.Application.Patients;

public sealed class CreatePatientHandler(IPatientRepository patients, TimeProvider time)
{
    public async Task<Result<PatientResponse>> HandleAsync(CreatePatientCommand command, CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var errors = CreatePatientValidator.Validate(command, DateOnly.FromDateTime(now.UtcDateTime));
        if (errors.Count > 0)
        {
            return Result.Failure<PatientResponse>(new BusinessError("validation.failed", ErrorKind.Validation, errors));
        }

        var patient = new Patient
        {
            Id = Guid.CreateVersion7(),
            FullName = command.FullName!.Trim(),
            DocumentNumber = command.DocumentNumber!.Trim(),
            Phone = command.Phone!.Trim(),
            BirthDate = command.BirthDate,
            CreatedAt = now,
        };

        if (!await patients.TryAddAsync(patient, ct))
        {
            return Result.Failure<PatientResponse>(new BusinessError("patient.duplicate_document", ErrorKind.Conflict));
        }

        return Result.Success(PatientResponse.From(patient));
    }
}
