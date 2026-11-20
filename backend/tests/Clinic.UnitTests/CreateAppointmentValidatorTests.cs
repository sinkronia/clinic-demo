using Clinic.Application.Appointments;

namespace Clinic.UnitTests;

public class CreateAppointmentValidatorTests
{
    private static readonly DateTimeOffset _start = new(2026, 6, 16, 10, 0, 0, TimeSpan.Zero);

    private static CreateAppointmentCommand Valid(int minutes = 30) =>
        new(Guid.NewGuid(), Guid.NewGuid(), _start, _start.AddMinutes(minutes));

    [Fact]
    public void Validate_valid_command_has_no_errors()
    {
        Assert.Empty(CreateAppointmentValidator.Validate(Valid()));
    }

    [Theory]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(240)]
    public void Validate_duration_within_limits_is_valid(int minutes)
    {
        Assert.Empty(CreateAppointmentValidator.Validate(Valid(minutes)));
    }

    [Fact]
    public void Validate_duration_below_minimum_is_too_short()
    {
        var errors = CreateAppointmentValidator.Validate(Valid(14));
        Assert.Equal(["too_short"], errors["endsAt"]);
    }

    [Fact]
    public void Validate_duration_above_maximum_is_too_long()
    {
        var errors = CreateAppointmentValidator.Validate(Valid(241));
        Assert.Equal(["too_long"], errors["endsAt"]);
    }

    [Fact]
    public void Validate_end_equal_to_start_is_end_before_start()
    {
        var errors = CreateAppointmentValidator.Validate(Valid(0));
        Assert.Equal(["end_before_start"], errors["endsAt"]);
    }

    [Fact]
    public void Validate_end_before_start_is_end_before_start()
    {
        var errors = CreateAppointmentValidator.Validate(Valid(-30));
        Assert.Equal(["end_before_start"], errors["endsAt"]);
    }

    [Fact]
    public void Validate_missing_patient_is_required()
    {
        var errors = CreateAppointmentValidator.Validate(Valid() with { PatientId = Guid.Empty });
        Assert.Equal(["required"], errors["patientId"]);
    }

    [Fact]
    public void Validate_missing_dental_chair_is_required()
    {
        var errors = CreateAppointmentValidator.Validate(Valid() with { DentalChairId = Guid.Empty });
        Assert.Equal(["required"], errors["dentalChairId"]);
    }

    [Fact]
    public void Validate_missing_start_is_required()
    {
        var errors = CreateAppointmentValidator.Validate(Valid() with { StartsAt = default });
        Assert.Equal(["required"], errors["startsAt"]);
    }

    [Fact]
    public void Validate_missing_end_is_required()
    {
        var errors = CreateAppointmentValidator.Validate(Valid() with { EndsAt = default });
        Assert.Equal(["required"], errors["endsAt"]);
    }
}
