namespace Clinic.Application.Appointments;

public static class CreateAppointmentValidator
{
    public const int MinDurationMinutes = 15;
    public const int MaxDurationMinutes = 240;

    /// <summary>Returns field name to error codes. Empty when the command is valid.</summary>
    public static Dictionary<string, string[]> Validate(CreateAppointmentCommand command)
    {
        var errors = new Dictionary<string, string[]>();

        if (command.PatientId == Guid.Empty)
        {
            errors["patientId"] = ["required"];
        }

        if (command.DentalChairId == Guid.Empty)
        {
            errors["dentalChairId"] = ["required"];
        }

        if (command.StartsAt == default)
        {
            errors["startsAt"] = ["required"];
        }

        if (command.EndsAt == default)
        {
            errors["endsAt"] = ["required"];
        }

        if (command.StartsAt != default && command.EndsAt != default)
        {
            if (command.EndsAt <= command.StartsAt)
            {
                errors["endsAt"] = ["end_before_start"];
            }
            else
            {
                var duration = command.EndsAt - command.StartsAt;
                if (duration < TimeSpan.FromMinutes(MinDurationMinutes))
                {
                    errors["endsAt"] = ["too_short"];
                }
                else if (duration > TimeSpan.FromMinutes(MaxDurationMinutes))
                {
                    errors["endsAt"] = ["too_long"];
                }
            }
        }

        return errors;
    }
}
