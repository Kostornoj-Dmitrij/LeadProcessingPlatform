using AutoFixture.NUnit3;
using LeadService.Application.Queries.GetLeadsByStatus;
using LeadService.Domain.Entities;
using LeadService.Domain.Enums;
using LeadService.Tests.Common.Attributes;
using LeadService.Tests.Common.Database;
using Moq;
using NUnit.Framework;
using SharedKernel.Base;

namespace LeadService.Tests.Application.Queries;

/// <summary>
/// Тесты для GetLeadsByStatusQueryHandler
/// </summary>
[Category("Application")]
public class GetLeadsByStatusQueryHandlerTests : DatabaseTestBase
{
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private GetLeadsByStatusQueryHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _sut = new GetLeadsByStatusQueryHandler(_unitOfWorkMock.Object);
    }

    [TearDown]
    public void Cleanup()
    {
        _unitOfWorkMock.Reset();
    }

    [Test, AutoData]
    public async Task Handle_WithoutFilters_ShouldReturnAllLeadsOrderedByCreatedAtDesc(
        [WithValidLead] Lead lead1,
        [WithValidLead] Lead lead2,
        [WithValidLead] Lead lead3)
    {
        var leads = new List<Lead> { lead1, lead2, lead3 };
        var query = new GetLeadsByStatusQuery();
        var leadSetMock = CreateMockDbSet(leads);

        _unitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        var result = await _sut.Handle(query, CancellationToken.None);

        Assert.That(result.Count, Is.EqualTo(leads.Count));

        var expectedOrder = leads.OrderByDescending(x => x.CreatedAt).Select(x => x.Id).ToList();
        var actualOrder = result.Select(x => x.Id).ToList();

        Assert.That(actualOrder, Is.EqualTo(expectedOrder));
    }

    [Test, AutoData]
    public async Task Handle_ShouldMapCustomFieldsCorrectly(
        [WithValidLead] Lead lead)
    {
        var query = new GetLeadsByStatusQuery();
        var leads = new List<Lead> { lead };
        var leadSetMock = CreateMockDbSet(leads);

        _unitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        var result = await _sut.Handle(query, CancellationToken.None);

        var resultLead = result.First();
        Assert.That(resultLead.CustomFields, Is.Not.Null);
        Assert.That(resultLead.CustomFields.Count, Is.EqualTo(lead.CustomFields.Count));

        foreach (var field in lead.CustomFields)
        {
            Assert.That(resultLead.CustomFields.ContainsKey(field.FieldName), Is.True);
            Assert.That(resultLead.CustomFields[field.FieldName], Is.EqualTo(field.FieldValue));
        }
    }

    [Test, AutoData]
    public async Task Handle_WithStatusFilter_ShouldReturnOnlyLeadsWithSpecifiedStatus(
        [WithValidLead(LeadStatus.Qualified)] Lead qualifiedLead1,
        [WithValidLead(LeadStatus.Qualified)] Lead qualifiedLead2,
        [WithValidLead(LeadStatus.Rejected)] Lead rejectedLead,
        [WithValidLead(LeadStatus.Distributed)] Lead distributedLead)
    {
        qualifiedLead1.GetType().GetProperty(nameof(Lead.CreatedAt))?.SetValue(qualifiedLead1, DateTime.UtcNow.AddHours(-3));
        qualifiedLead2.GetType().GetProperty(nameof(Lead.CreatedAt))?.SetValue(qualifiedLead2, DateTime.UtcNow.AddHours(-2));
        rejectedLead.GetType().GetProperty(nameof(Lead.CreatedAt))?.SetValue(rejectedLead, DateTime.UtcNow.AddHours(-1));
        distributedLead.GetType().GetProperty(nameof(Lead.CreatedAt))?.SetValue(distributedLead, DateTime.UtcNow.AddHours(-4));

        var leads = new List<Lead> { qualifiedLead1, qualifiedLead2, rejectedLead, distributedLead };
        var query = new GetLeadsByStatusQuery { Status = LeadStatus.Qualified };
        var leadSetMock = CreateMockDbSet(leads);

        _unitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        var result = await _sut.Handle(query, CancellationToken.None);

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.All(x => x.Status == nameof(LeadStatus.Qualified)), Is.True);
        Assert.That(result[0].Id, Is.EqualTo(qualifiedLead2.Id));
        Assert.That(result[1].Id, Is.EqualTo(qualifiedLead1.Id));
    }

    [Test, AutoData]
    public async Task Handle_WithOffset_ShouldSkipSpecifiedNumberOfLeads(
        [WithValidLead] Lead lead1,
        [WithValidLead] Lead lead2,
        [WithValidLead] Lead lead3)
    {
        lead1.GetType().GetProperty(nameof(Lead.CreatedAt))?.SetValue(lead1, DateTime.UtcNow.AddHours(-3));
        lead2.GetType().GetProperty(nameof(Lead.CreatedAt))?.SetValue(lead2, DateTime.UtcNow.AddHours(-2));
        lead3.GetType().GetProperty(nameof(Lead.CreatedAt))?.SetValue(lead3, DateTime.UtcNow.AddHours(-1));

        var leads = new List<Lead> { lead1, lead2, lead3 };
        var query = new GetLeadsByStatusQuery { Offset = 1 };
        var leadSetMock = CreateMockDbSet(leads);

        _unitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        var result = await _sut.Handle(query, CancellationToken.None);

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].Id, Is.EqualTo(lead3.Id));
        Assert.That(result[1].Id, Is.EqualTo(lead2.Id));
    }

    [Test, AutoData]
    public async Task Handle_WithLimit_ShouldReturnOnlySpecifiedNumberOfLeads(
        [WithValidLead] Lead lead1,
        [WithValidLead] Lead lead2,
        [WithValidLead] Lead lead3)
    {
        var leads = new List<Lead> { lead1, lead2, lead3 };
        var query = new GetLeadsByStatusQuery { Limit = 2 };
        var leadSetMock = CreateMockDbSet(leads);

        _unitOfWorkMock
            .Setup(x => x.Set<Lead>())
            .Returns(leadSetMock.Object);

        var result = await _sut.Handle(query, CancellationToken.None);

        Assert.That(result.Count, Is.EqualTo(2));
    }
}