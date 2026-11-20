namespace Clinic.Application.Appointments;

public static class CreateAppointmentValidator
{
    public const int MinMinutes = 15;
    public const int MaxMinutes = 240;

    /// <summary>Returns field name to error codes. Empty when the command is valid.</summary>
    public static Dictionary<string, string[]> Validate(CreateAppointmentCommand command)
    {
        var errors = new Dictionary<string, string[]>();

        if (command.PatientId is null || command.PatientId == Guid.Empty)
        {
            errors["patientId"] = ["required"];
        }

        if (command.DentalChairId is null || command.DentalChairId == Guid.Empty)
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
        else if (command.StartsAt != default)
        {
            var duration = command.EndsAt - command.StartsAt;
            if (duration <= TimeSpan.Zero)
            {
                errors["endsAt"] = ["must_be_after_start"];
            }
            else if (duration < TimeSpan.FromMinutes(MinMinutes))
            {
                errors["endsAt"] = ["too_short"];
            }
            else if (duration > TimeSpan.FromMinutes(MaxMinutes))
            {
                errors["endsAt"] = ["too_long"];
            }
        }

        return errors;
    }
}
