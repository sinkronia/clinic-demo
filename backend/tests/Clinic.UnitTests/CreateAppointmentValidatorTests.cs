using Clinic.Application.Appointments;

namespace Clinic.UnitTests;

public class CreateAppointmentValidatorTests
{
    private static readonly DateTimeOffset _start = new(2026, 6, 16, 10, 0, 0, TimeSpan.Zero);

    private static CreateAppointmentCommand Valid() =>
        new(Guid.NewGuid(), Guid.NewGuid(), _start, _start.AddMinutes(30));

    [Fact]
    public void Validate_valid_command_has_no_errors()
    {
        Assert.Empty(CreateAppointmentValidator.Validate(Valid()));
    }

    [Theory]
    [InlineData(15)]
    [InlineData(240)]
    public void Validate_duration_on_the_limits_is_valid(int minutes)
    {
        Assert.Empty(CreateAppointmentValidator.Validate(Valid() with { EndsAt = _start.AddMinutes(minutes) }));
    }

    [Fact]
    public void Validate_duration_under_15_minutes_is_too_short()
    {
        var errors = CreateAppointmentValidator.Validate(Valid() with { EndsAt = _start.AddMinutes(14) });
        Assert.Equal(["too_short"], errors["endsAt"]);
    }

    [Fact]
    public void Validate_duration_over_240_minutes_is_too_long()
    {
        var errors = CreateAppointmentValidator.Validate(Valid() with { EndsAt = _start.AddMinutes(241) });
        Assert.Equal(["too_long"], errors["endsAt"]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    public void Validate_end_not_after_start_is_rejected(int minutes)
    {
        var errors = CreateAppointmentValidator.Validate(Valid() with { EndsAt = _start.AddMinutes(minutes) });
        Assert.Equal(["must_be_after_start"], errors["endsAt"]);
    }

    [Fact]
    public void Validate_missing_fields_are_required()
    {
        var errors = CreateAppointmentValidator.Validate(new CreateAppointmentCommand(null, Guid.Empty, default, default));

        Assert.Equal(["required"], errors["patientId"]);
        Assert.Equal(["required"], errors["dentalChairId"]);
        Assert.Equal(["required"], errors["startsAt"]);
        Assert.Equal(["required"], errors["endsAt"]);
    }
}
