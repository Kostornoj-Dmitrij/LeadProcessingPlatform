using AutoFixture.NUnit4;
using LeadService.Application.Common.Behaviors;
using LeadService.Tests.Common.TestData;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SharedKernel.Base;

namespace LeadService.Tests.Application.Behaviors;

/// <summary>
/// Тесты для TransactionBehavior
/// </summary>
[Category("Application")]
public class TransactionBehaviorTests
{
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private Mock<ILogger<TransactionBehavior<IRequest<Unit>, Unit>>> _loggerMock = null!;
    private TransactionBehavior<IRequest<Unit>, Unit> _sut = null!;

    [SetUp]
    public void Setup()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<TransactionBehavior<IRequest<Unit>, Unit>>>();
        _sut = new TransactionBehavior<IRequest<Unit>, Unit>(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    [TearDown]
    public void Cleanup()
    {
        _unitOfWorkMock.Reset();
        _loggerMock.Reset();
    }

    [Test, AutoData]
    public async Task Handle_WhenRequestIsNotCommand_ShouldNotBeginTransaction(
        TestRequest request)
    {
        var response = Unit.Value;
        Task<Unit> Next(CancellationToken _) => Task.FromResult(response);
        RequestHandlerDelegate<Unit> next = Next;

        var result = await _sut.Handle(request, next, CancellationToken.None);

        Assert.That(result, Is.EqualTo(response));

        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(
            It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(
            It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test, AutoData]
    public async Task Handle_WhenRequestIsCommand_ShouldBeginAndCommitTransaction(
        TestCommand command)
    {
        var response = Unit.Value;
        Task<Unit> Next(CancellationToken _) => Task.FromResult(response);
        RequestHandlerDelegate<Unit> next = Next;

        var result = await _sut.Handle(command, next, CancellationToken.None);

        Assert.That(result, Is.EqualTo(response));

        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test, AutoData]
    public void Handle_WhenCommandThrows_ShouldRollbackTransaction(
        TestCommand command,
        InvalidOperationException exception)
    {
        Task<Unit> Next(CancellationToken _) => Task.FromException<Unit>(exception);
        RequestHandlerDelegate<Unit> next = Next;

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.Handle(command, next, CancellationToken.None));

        Assert.That(ex.Message, Is.EqualTo(exception.Message));

        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(
            It.IsAny<CancellationToken>()), Times.Never);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("Transaction rolled back")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}