using System.Text.Json;
using AutoFixture.NUnit4;
using LeadService.Application.Commands.CreateLead;
using LeadService.Application.Common.DTOs;
using LeadService.Application.Common.Interfaces;
using LeadService.Domain.Entities;
using LeadService.Tests.Common.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SharedKernel.Base;
using SharedKernel.Entities;
using SharedKernel.Json;

namespace LeadService.Tests.Application.Commands.CreateLead;

/// <summary>
/// Тесты для CreateLeadCommandHandler
/// </summary>
[Category("Application")]
public class CreateLeadCommandHandlerTests
{
    private const string InvalidResponse = "{\"id\":\"00000000-0000-0000-0000-000000000000\",\"source\":\"test\"}";
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private Mock<IIdempotencyRepository> _idempotencyRepoMock = null!;
    private Mock<ILogger<CreateLeadCommandHandler>> _loggerMock = null!;
    private CreateLeadCommandHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _idempotencyRepoMock = new Mock<IIdempotencyRepository>();
        _loggerMock = new Mock<ILogger<CreateLeadCommandHandler>>();

        _sut = new CreateLeadCommandHandler(
            _unitOfWorkMock.Object,
            _idempotencyRepoMock.Object,
            _loggerMock.Object);
    }

    [TearDown]
    public void Cleanup()
    {
        _unitOfWorkMock.Reset();
        _idempotencyRepoMock.Reset();
        _loggerMock.Reset();
    }

    [Test, AutoData]
    public async Task Handle_WithExternalId_ShouldAcquireLockAndCreateLead(
        [WithValidLeadCommand] CreateLeadCommand command,
        IdempotencyKey idempotencyKey)
    {
        idempotencyKey.Key = command.ExternalLeadId!;
        idempotencyKey.LockedUntil = DateTime.UtcNow.AddMinutes(5);
        idempotencyKey.ResponseCode = null;

        _idempotencyRepoMock
            .Setup(x => x.TryAcquireLockAsync(
                command.ExternalLeadId!,
                It.IsAny<byte[]>(),
                TimeSpan.FromSeconds(30),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(idempotencyKey);

        _unitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(Mock.Of<DbSet<Lead>>());

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.That(result, Is.Not.Null);

        _idempotencyRepoMock.Verify(
            x => x.TryAcquireLockAsync(
                command.ExternalLeadId!,
                It.IsAny<byte[]>(),
                TimeSpan.FromSeconds(30),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _idempotencyRepoMock.Verify(
            x => x.UpdateWithResultAsync(
                idempotencyKey.Id,
                It.IsAny<Guid>(),
                200,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test, AutoData]
    public void Handle_WhenLockAcquisitionFails_ShouldThrow(
        [WithValidLeadCommand] CreateLeadCommand command)
    {
        _idempotencyRepoMock
            .Setup(x => x.TryAcquireLockAsync(
                command.ExternalLeadId!,
                It.IsAny<byte[]>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdempotencyKey?)null);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.Handle(command, CancellationToken.None));

        Assert.That(ex.Message,
            Is.EqualTo("Request is being processed by another instance"));
    }

    [Test, AutoData]
    public async Task Handle_WithCachedResponse_ShouldReturnCachedResult(
        [WithValidLeadCommand] CreateLeadCommand command,
        LeadDto cachedLeadDto,
        IdempotencyKey idempotencyKey)
    {
        var cachedResponse = JsonSerializer.Serialize(cachedLeadDto, JsonDefaults.Options);

        idempotencyKey.Key = command.ExternalLeadId!;
        idempotencyKey.ResponseCode = 200;
        idempotencyKey.ResponseBody = cachedResponse;
        idempotencyKey.LockedUntil = null;

        _idempotencyRepoMock
            .Setup(x => x.TryAcquireLockAsync(
                command.ExternalLeadId!,
                It.IsAny<byte[]>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(idempotencyKey);

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.That(result.Id, Is.EqualTo(cachedLeadDto.Id));
        Assert.That(result.Source, Is.EqualTo(cachedLeadDto.Source));

        _unitOfWorkMock.Verify(
            x => x.Set<Lead>().AddAsync(It.IsAny<Lead>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test, AutoData]
    public async Task Handle_WithCachedInvalidResponse_ShouldReprocess(
        [WithValidLeadCommand] CreateLeadCommand command,
        IdempotencyKey idempotencyKey)
    {
        idempotencyKey.Key = command.ExternalLeadId!;
        idempotencyKey.ResponseCode = 200;
        idempotencyKey.ResponseBody = InvalidResponse;
        idempotencyKey.LockedUntil = null;

        _idempotencyRepoMock
            .Setup(x => x.TryAcquireLockAsync(
                command.ExternalLeadId!,
                It.IsAny<byte[]>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(idempotencyKey);

        _unitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(Mock.Of<DbSet<Lead>>());

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));

        _idempotencyRepoMock.Verify(
            x => x.UpdateWithResultAsync(
                idempotencyKey.Id,
                It.IsAny<Guid>(),
                200,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("Cached response for key")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
            Times.Once);
    }

    [Test, AutoData]
    public void Handle_WhenCreateLeadInternalThrows_ShouldReleaseLock(
        [WithValidLeadCommand] CreateLeadCommand command,
        IdempotencyKey idempotencyKey)
    {
        idempotencyKey.Key = command.ExternalLeadId!;
        idempotencyKey.LockedUntil = DateTime.UtcNow.AddMinutes(5);
        idempotencyKey.ResponseCode = null;

        _idempotencyRepoMock
            .Setup(x => x.TryAcquireLockAsync(
                command.ExternalLeadId!,
                It.IsAny<byte[]>(),
                TimeSpan.FromSeconds(30),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(idempotencyKey);

        _unitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Throws(new InvalidOperationException("Database error"));

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.Handle(command, CancellationToken.None));

        Assert.That(ex.Message, Is.EqualTo("Database error"));

        _idempotencyRepoMock.Verify(
            x => x.ReleaseLockAsync(
                idempotencyKey.Id,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WithoutExternalId_ShouldCreateLeadDirectly(
        [WithValidLeadCommand] CreateLeadCommand command)
    {
        command.ExternalLeadId = null;
        var leadAdded = false;

        _unitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(Mock.Of<DbSet<Lead>>());

        _unitOfWorkMock
            .Setup(x => x.Set<Lead>().AddAsync(It.IsAny<Lead>(), It.IsAny<CancellationToken>()))
            .Callback<Lead, CancellationToken>((_, _) => leadAdded = true)
            .ReturnsAsync((Lead _, CancellationToken _) => null!);

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.That(leadAdded, Is.True);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Source, Is.EqualTo(command.Source));
        Assert.That(result.CompanyName, Is.EqualTo(command.CompanyName));
        Assert.That(result.Email, Is.EqualTo(command.Email));

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _idempotencyRepoMock.Verify(
            x => x.TryAcquireLockAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}