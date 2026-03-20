using AutoFixture.NUnit3;
using FluentValidation;
using FluentValidation.Results;
using LeadService.Application.Common.Behaviors;
using LeadService.Tests.Common.TestData;
using MediatR;
using Moq;
using NUnit.Framework;

namespace LeadService.Tests.Application.Behaviors;

/// <summary>
/// Тесты для ValidationBehavior
/// </summary>
[Category("Application")]
public class ValidationBehaviorTests
{
    private Mock<IValidator<TestRequest>> _validatorMock = null!;
    private ValidationBehavior<TestRequest, Unit> _sut = null!;

    [SetUp]
    public void Setup()
    {
        _validatorMock = new Mock<IValidator<TestRequest>>();
    }

    [TearDown]
    public void Cleanup()
    {
        _validatorMock.Reset();
    }

    [Test, AutoData]
    public async Task Handle_WhenNoValidators_ShouldCallNext(
        TestRequest request)
    {
        var validators = Enumerable.Empty<IValidator<TestRequest>>();
        _sut = new ValidationBehavior<TestRequest, Unit>(validators);

        var nextCalled = false;
        Task<Unit> Next(CancellationToken _)
        {
            nextCalled = true;
            return Task.FromResult(Unit.Value);
        }
        RequestHandlerDelegate<Unit> next = Next;

        var result = await _sut.Handle(request, next, CancellationToken.None);

        Assert.That(nextCalled, Is.True);
        Assert.That(result, Is.EqualTo(Unit.Value));
    }

    [Test, AutoData]
    public async Task Handle_WhenValidatorsPass_ShouldCallNext(
        TestRequest request)
    {
        _validatorMock
            .Setup(x =>
                x.ValidateAsync(
                    It.IsAny<ValidationContext<TestRequest>>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var validators = new[] { _validatorMock.Object };
        _sut = new ValidationBehavior<TestRequest, Unit>(validators);

        var nextCalled = false;
        Task<Unit> Next(CancellationToken _)
        {
            nextCalled = true;
            return Task.FromResult(Unit.Value);
        }
        RequestHandlerDelegate<Unit> next = Next;

        var result = await _sut.Handle(request, next, CancellationToken.None);

        Assert.That(nextCalled, Is.True);
        Assert.That(result, Is.EqualTo(Unit.Value));

        _validatorMock.Verify(
            x => x.ValidateAsync(
                It.Is<ValidationContext<TestRequest>>(
                    ctx => ctx.InstanceToValidate == request),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test, AutoData]
    public void Handle_WhenValidatorsFail_ShouldThrowValidationException(
        TestRequest request,
        List<ValidationFailure> failures)
    {
        _validatorMock
            .Setup(x =>
                x.ValidateAsync(
                    It.IsAny<ValidationContext<TestRequest>>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var validators = new[] { _validatorMock.Object };
        _sut = new ValidationBehavior<TestRequest, Unit>(validators);

        Task<Unit> Next(CancellationToken _) => Task.FromResult(Unit.Value);
        RequestHandlerDelegate<Unit> next = Next;

        var ex = Assert.ThrowsAsync<ValidationException>(() =>
            _sut.Handle(request, next, CancellationToken.None));

        Assert.That(ex.Errors.Count(), Is.EqualTo(failures.Count));
        foreach (var failure in failures)
        {
            Assert.That(ex.Errors.Select(e => e.ErrorMessage),
                Contains.Item(failure.ErrorMessage));
        }
    }
}