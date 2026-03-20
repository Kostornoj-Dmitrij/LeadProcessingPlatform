using AutoFixture.NUnit3;
using LeadService.Application.Queries.GetLeadById;
using LeadService.Domain.Entities;
using LeadService.Tests.Common.Attributes;
using LeadService.Tests.Common.Database;
using Moq;
using NUnit.Framework;
using SharedKernel.Base;

namespace LeadService.Tests.Application.Queries;

/// <summary>
/// Тесты для GetLeadByIdQueryHandler
/// </summary>
[Category("Application")]
public class GetLeadByIdQueryHandlerTests : DatabaseTestBase
{
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private GetLeadByIdQueryHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _sut = new GetLeadByIdQueryHandler(_unitOfWorkMock.Object);
    }

    [TearDown]
    public void Cleanup()
    {
        _unitOfWorkMock.Reset();
    }

    [Test, AutoData]
    public async Task Handle_WhenLeadExists_ShouldReturnLeadDto(
        [WithValidLead] Lead lead,
        GetLeadByIdQuery query)
    {
        var leadType = typeof(Lead);
        leadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, query.Id);

        var leads = new List<Lead> { lead };
        var leadSetMock = CreateMockDbSet(leads);

        _unitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        var result = await _sut.Handle(query, CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(lead.Id));
        Assert.That(result.Source, Is.EqualTo(lead.Source));
        Assert.That(result.CompanyName, Is.EqualTo(lead.CompanyName.Value));
        Assert.That(result.Email, Is.EqualTo(lead.Email.Value));
        Assert.That(result.ContactPerson, Is.EqualTo(lead.ContactPerson));
        Assert.That(result.Phone, Is.EqualTo(lead.Phone?.Value));
        Assert.That(result.ExternalLeadId, Is.EqualTo(lead.ExternalLeadId));
        Assert.That(result.Status, Is.EqualTo(lead.Status.ToString()));
        Assert.That(result.Score, Is.EqualTo(lead.Score));
        Assert.That(result.CustomFields?.Count, Is.EqualTo(lead.CustomFields.Count));
    }

    [Test, AutoData]
    public async Task Handle_WhenLeadNotFound_ShouldReturnNull(
        GetLeadByIdQuery query)
    {
        var leads = new List<Lead>();
        var leadSetMock = CreateMockDbSet(leads);

        _unitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        var result = await _sut.Handle(query, CancellationToken.None);

        Assert.That(result, Is.Null);
    }

    [Test, AutoData]
    public async Task Handle_ShouldIncludeCustomFields(
        [WithValidLead] Lead lead,
        GetLeadByIdQuery query)
    {
        var leadType = typeof(Lead);
        leadType.GetProperty(nameof(Lead.Id))?.SetValue(lead, query.Id);

        var leads = new List<Lead> { lead };
        var leadSetMock = CreateMockDbSet(leads);

        _unitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        var result = await _sut.Handle(query, CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.CustomFields, Is.Not.Null);

        foreach (var field in lead.CustomFields)
        {
            Assert.That(result.CustomFields.ContainsKey(field.FieldName), Is.True);
            Assert.That(result.CustomFields[field.FieldName], Is.EqualTo(field.FieldValue));
        }
    }
}