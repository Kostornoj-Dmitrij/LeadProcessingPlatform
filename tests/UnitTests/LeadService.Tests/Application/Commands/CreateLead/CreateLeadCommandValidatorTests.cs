using AutoFixture.NUnit3;
using LeadService.Application.Commands.CreateLead;
using LeadService.Tests.Common.Attributes;
using NUnit.Framework;

namespace LeadService.Tests.Application.Commands.CreateLead;

/// <summary>
/// Тесты для CreateLeadCommandValidator
/// </summary>
[Category("Application")]
public class CreateLeadCommandValidatorTests
{
    private CreateLeadCommandValidator _sut = null!;

    [SetUp]
    public void Setup()
    {
        _sut = new CreateLeadCommandValidator();
    }

    [Test, AutoData]
    public void Validate_WithValidCommand_ShouldNotHaveErrors(
        [WithValidLeadCommand] CreateLeadCommand command)
    {
        var result = _sut.Validate(command);
        
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test, AutoData]
    public void Validate_WhenSourceEmpty_ShouldHaveError(
        [WithValidLeadCommand] CreateLeadCommand command)
    {
        command.Source = string.Empty;

        var result = _sut.Validate(command);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.One.Matches<FluentValidation.Results.ValidationFailure>(
            x => x is { PropertyName: nameof(CreateLeadCommand.Source),
                ErrorMessage: "Source is required" }));
    }

    [Test, AutoData]
    public void Validate_WhenSourceTooLong_ShouldHaveError(
        [WithValidLeadCommand] CreateLeadCommand command)
    {
        command.Source = new string('a', 101);

        var result = _sut.Validate(command);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.One.Matches<FluentValidation.Results.ValidationFailure>(
            x => x is { PropertyName: nameof(CreateLeadCommand.Source),
                ErrorMessage: "Source must not exceed 100 characters" }));
    }

    [Test, AutoData]
    public void Validate_WhenCompanyNameEmpty_ShouldHaveError(
        [WithValidLeadCommand] CreateLeadCommand command)
    {
        command.CompanyName = string.Empty;

        var result = _sut.Validate(command);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.One.Matches<FluentValidation.Results.ValidationFailure>(
            x => x is { PropertyName: nameof(CreateLeadCommand.CompanyName),
                ErrorMessage: "Company name is required" }));
    }

    [Test, AutoData]
    public void Validate_WhenCompanyNameTooLong_ShouldHaveError(
        [WithValidLeadCommand] CreateLeadCommand command)
    {
        command.CompanyName = new string('a', 256);

        var result = _sut.Validate(command);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.One.Matches<FluentValidation.Results.ValidationFailure>(
            x => x is { PropertyName: nameof(CreateLeadCommand.CompanyName),
                ErrorMessage: "Company name must not exceed 255 characters" }));
    }

    [Test, AutoData]
    public void Validate_WhenEmailEmpty_ShouldHaveError(
        [WithValidLeadCommand] CreateLeadCommand command)
    {
        command.Email = string.Empty;

        var result = _sut.Validate(command);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.One.Matches<FluentValidation.Results.ValidationFailure>(
            x => x is { PropertyName: nameof(CreateLeadCommand.Email),
                ErrorMessage: "Email is required" }));
    }

    [Test, AutoData]
    public void Validate_WhenEmailInvalidFormat_ShouldHaveError(
        [WithValidLeadCommand] CreateLeadCommand command)
    {
        command.Email = "not-an-email";

        var result = _sut.Validate(command);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.One.Matches<FluentValidation.Results.ValidationFailure>(
            x => x is { PropertyName: nameof(CreateLeadCommand.Email),
                ErrorMessage: "Invalid email format" }));
    }

    [Test, AutoData]
    public void Validate_WhenPhoneTooLong_ShouldHaveError(
        [WithValidLeadCommand] CreateLeadCommand command)
    {
        command.Phone = new string('1', 51);

        var result = _sut.Validate(command);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.One.Matches<FluentValidation.Results.ValidationFailure>(
            x => x is { PropertyName: nameof(CreateLeadCommand.Phone),
                ErrorMessage: "Phone must not exceed 50 characters" }));
    }

    [Test, AutoData]
    public void Validate_WhenExternalLeadIdTooLong_ShouldHaveError(
        [WithValidLeadCommand] CreateLeadCommand command)
    {
        command.ExternalLeadId = new string('a', 256);

        var result = _sut.Validate(command);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.One.Matches<FluentValidation.Results.ValidationFailure>(
            x => x is { PropertyName: nameof(CreateLeadCommand.ExternalLeadId),
                ErrorMessage: "ExternalLeadId must not exceed 255 characters" }));
    }
}