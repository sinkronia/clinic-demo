using Clinic.Application.Patients;

namespace Clinic.UnitTests;

public class CreatePatientValidatorTests
{
    private static readonly DateOnly _today = new(2026, 6, 15);

    private static CreatePatientCommand Valid() => new("Test Patient", "1234567", "+59170000000", new DateOnly(1990, 5, 20));

    [Fact]
    public void Validate_valid_command_has_no_errors()
    {
        Assert.Empty(CreatePatientValidator.Validate(Valid(), _today));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_missing_full_name_is_required(string? name)
    {
        var errors = CreatePatientValidator.Validate(Valid() with { FullName = name }, _today);
        Assert.Equal(["required"], errors["fullName"]);
    }

    [Fact]
    public void Validate_long_full_name_is_too_long()
    {
        var errors = CreatePatientValidator.Validate(Valid() with { FullName = new string('a', 201) }, _today);
        Assert.Equal(["too_long"], errors["fullName"]);
    }

    [Theory]
    [InlineData("12")]
    [InlineData("abcdefg")]
    [InlineData("12345678901")]
    public void Validate_bad_document_is_invalid_format(string document)
    {
        var errors = CreatePatientValidator.Validate(Valid() with { DocumentNumber = document }, _today);
        Assert.Equal(["invalid_format"], errors["documentNumber"]);
    }

    [Theory]
    [InlineData("1234567")]
    [InlineData("1234567-1A")]
    public void Validate_document_with_optional_complement_is_valid(string document)
    {
        Assert.Empty(CreatePatientValidator.Validate(Valid() with { DocumentNumber = document }, _today));
    }

    [Fact]
    public void Validate_bad_phone_is_invalid_format()
    {
        var errors = CreatePatientValidator.Validate(Valid() with { Phone = "call me" }, _today);
        Assert.Equal(["invalid_format"], errors["phone"]);
    }

    [Fact]
    public void Validate_future_birth_date_is_in_future()
    {
        var errors = CreatePatientValidator.Validate(Valid() with { BirthDate = _today.AddDays(1) }, _today);
        Assert.Equal(["in_future"], errors["birthDate"]);
    }

    [Fact]
    public void Validate_birth_date_today_is_valid()
    {
        Assert.Empty(CreatePatientValidator.Validate(Valid() with { BirthDate = _today }, _today));
    }

    [Fact]
    public void Validate_missing_birth_date_is_required()
    {
        var errors = CreatePatientValidator.Validate(Valid() with { BirthDate = default }, _today);
        Assert.Equal(["required"], errors["birthDate"]);
    }
}
