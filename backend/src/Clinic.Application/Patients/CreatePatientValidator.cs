using System.Text.RegularExpressions;

namespace Clinic.Application.Patients;

public static partial class CreatePatientValidator
{
    public const int FullNameMaxLength = 200;

    /// <summary>Returns field name to error codes. Empty when the command is valid.</summary>
    public static Dictionary<string, string[]> Validate(CreatePatientCommand command, DateOnly today)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(command.FullName))
        {
            errors["fullName"] = ["required"];
        }
        else if (command.FullName.Trim().Length > FullNameMaxLength)
        {
            errors["fullName"] = ["too_long"];
        }

        if (string.IsNullOrWhiteSpace(command.DocumentNumber))
        {
            errors["documentNumber"] = ["required"];
        }
        else if (!DocumentRegex().IsMatch(command.DocumentNumber.Trim()))
        {
            errors["documentNumber"] = ["invalid_format"];
        }

        if (string.IsNullOrWhiteSpace(command.Phone))
        {
            errors["phone"] = ["required"];
        }
        else if (!PhoneRegex().IsMatch(command.Phone.Trim()))
        {
            errors["phone"] = ["invalid_format"];
        }

        if (command.BirthDate == default)
        {
            errors["birthDate"] = ["required"];
        }
        else if (command.BirthDate > today)
        {
            errors["birthDate"] = ["in_future"];
        }
        else if (command.BirthDate.Year < 1900)
        {
            errors["birthDate"] = ["too_old"];
        }

        return errors;
    }

    // Bolivian CI: 5 to 10 digits, optional complement such as "-1A".
    [GeneratedRegex(@"^\d{5,10}(-[0-9A-Z]{1,2})?$")]
    private static partial Regex DocumentRegex();

    [GeneratedRegex(@"^\+?\d{7,15}$")]
    private static partial Regex PhoneRegex();
}
