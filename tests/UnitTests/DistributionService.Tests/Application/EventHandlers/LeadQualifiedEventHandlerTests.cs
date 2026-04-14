using AutoFixture.NUnit3;
using AvroSchemas.Messages.LeadEvents;
using DistributionService.Application.EventHandlers;
using DistributionService.Domain.Entities;
using DistributionService.Domain.Enums;
using DistributionService.Tests.Common.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SharedTestInfrastructure.Database;

namespace DistributionService.Tests.Application.EventHandlers;

/// <summary>
/// Тесты для LeadQualifiedEventHandler
/// </summary>
[Category("Application")]
public class LeadQualifiedEventHandlerTests : DatabaseTestBase
{
    private static readonly Type RuleType = typeof(DistributionRule);
    private static readonly Type RequestType = typeof(DistributionRequest);

    private Mock<ILogger<LeadQualifiedEventHandler>> _loggerMock = null!;
    private LeadQualifiedEventHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        BaseSetup();
        _loggerMock = new Mock<ILogger<LeadQualifiedEventHandler>>();
        _sut = new LeadQualifiedEventHandler(UnitOfWorkMock.Object, _loggerMock.Object);
    }

    [TearDown]
    public void Cleanup()
    {
        BaseCleanup();
        _loggerMock.Reset();
    }

    #region Successful Request Creation

    [Test, AutoData]
    public async Task Handle_WhenRulesExistAndApplicable_ShouldCreateDistributionRequest(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());

        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWorkMock.Verify(x => x.Set<DistributionHistory>().AddAsync(
            It.IsAny<DistributionHistory>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test, AutoData]
    public async Task Handle_WhenExistingRequestExists_ShouldSkip(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule] DistributionRule rule,
        [WithValidDistributionRequest] DistributionRequest existingRequest)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());
        RequestType.GetProperty(nameof(DistributionRequest.LeadId))?.SetValue(existingRequest, @event.LeadId);

        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet([existingRequest]);
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(
                    $"Lead {@event.LeadId} already has a distribution request")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_ShouldCreateRequestWithCorrectData(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());

        DistributionRequest? createdRequest = null;
        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.Set<DistributionRequest>().AddAsync(
                It.IsAny<DistributionRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<DistributionRequest, CancellationToken>((req, _) => createdRequest = req)
            .ReturnsAsync((DistributionRequest _, CancellationToken _) => null!);

        await _sut.Handle(@event, CancellationToken.None);

        Assert.That(createdRequest, Is.Not.Null);
        Assert.That(createdRequest!.LeadId, Is.EqualTo(@event.LeadId));
        Assert.That(createdRequest.CompanyName, Is.EqualTo(@event.CompanyName));
        Assert.That(createdRequest.Email, Is.EqualTo(@event.Email));
        Assert.That(createdRequest.Score, Is.EqualTo(@event.Score));
        Assert.That(createdRequest.ContactPerson, Is.EqualTo(@event.ContactPerson));
        Assert.That(createdRequest.CustomFields, Is.EqualTo(@event.CustomFields));
        Assert.That(createdRequest.RuleId, Is.EqualTo(rule.Id));
        Assert.That(createdRequest.Status, Is.EqualTo(DistributionRequestStatus.Pending));
        Assert.That(createdRequest.RetryCount, Is.EqualTo(0));
    }

    #endregion

    #region Rule Evaluation - No Rules

    [Test, AutoData]
    public async Task Handle_WhenNoRulesExist_ShouldRecordFailure(
        [WithValidLeadQualifiedEvent] LeadQualified @event)
    {
        var rules = new List<DistributionRule>();
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        UnitOfWorkMock.Verify(x => x.Set<DistributionHistory>().AddAsync(
            It.IsAny<DistributionHistory>(), It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No active distribution rules found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenNoApplicableRuleFound_ShouldRecordFailure(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule(DistributionRuleStrategy.ScoreBased, "{\"type\":\"score_threshold\",\"min_score\":100}")] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());

        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        UnitOfWorkMock.Verify(x => x.Set<DistributionHistory>().AddAsync(
            It.IsAny<DistributionHistory>(), It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Rule Evaluation - Score Threshold

    [Test, AutoData]
    public async Task Handle_WhenRuleHasScoreThresholdCondition_ShouldEvaluateCorrectly(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule(DistributionRuleStrategy.FixedTarget, "{\"type\":\"score_threshold\",\"min_score\":75}")] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());

        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenScoreThresholdConditionHasNoMinScore_ShouldEvaluateToTrue(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule(DistributionRuleStrategy.FixedTarget, "{\"type\":\"score_threshold\"}")] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());

        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenScoreThresholdConditionHasInvalidMinScore_ShouldNotMatch(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule(DistributionRuleStrategy.FixedTarget, "{\"type\":\"score_threshold\",\"min_score\":\"invalid\"}")] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());

        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        UnitOfWorkMock.Verify(x => x.Set<DistributionHistory>().AddAsync(
            It.IsAny<DistributionHistory>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Rule Evaluation - Industry Match

    [Test, AutoData]
    public async Task Handle_WhenRuleHasIndustryMatchConditionAndMatches_ShouldCreateRequest(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule(DistributionRuleStrategy.Territory, "{\"type\":\"industry_match\",\"industry\":\"Technology\"}")] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());

        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenIndustryMatchConditionHasNoIndustry_ShouldNotMatch(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule(DistributionRuleStrategy.Territory, "{\"type\":\"industry_match\"}")] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());

        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        UnitOfWorkMock.Verify(x => x.Set<DistributionHistory>().AddAsync(
            It.IsAny<DistributionHistory>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenIndustryMatchConditionAndIndustryIsNullOrEmpty_ShouldNotMatch(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule(DistributionRuleStrategy.Territory, "{\"type\":\"industry_match\",\"industry\":\"Technology\"}")] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());
        var enrichedDataNull = new EnrichedData { Industry = null!, CompanySize = "50-100", Version = 1 };
        @event.GetType().GetProperty(nameof(LeadQualified.EnrichedData))?.SetValue(@event, enrichedDataNull);

        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        UnitOfWorkMock.Verify(x => x.Set<DistributionHistory>().AddAsync(
            It.IsAny<DistributionHistory>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Rule Evaluation - Revenue Range

    [Test, AutoData]
    public async Task Handle_WhenRuleHasRevenueRangeConditionAndMatches_ShouldCreateRequest(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule(DistributionRuleStrategy.Territory, "{\"type\":\"revenue_range\",\"range\":\"$10M-$50M\"}")] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());

        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenRevenueRangeConditionHasNoRange_ShouldNotMatch(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule(DistributionRuleStrategy.Territory, "{\"type\":\"revenue_range\"}")] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());

        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        UnitOfWorkMock.Verify(x => x.Set<DistributionHistory>().AddAsync(
            It.IsAny<DistributionHistory>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenRevenueRangeConditionAndRevenueRangeIsNullOrEmpty_ShouldNotMatch(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule(DistributionRuleStrategy.Territory, "{\"type\":\"revenue_range\",\"range\":\"$10M-$50M\"}")] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());

        var enrichedDataNull = new EnrichedData { Industry = "Technology", CompanySize = "50-100", RevenueRange = null, Version = 1 };
        @event.GetType().GetProperty(nameof(LeadQualified.EnrichedData))?.SetValue(@event, enrichedDataNull);

        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        UnitOfWorkMock.Verify(x => x.Set<DistributionHistory>().AddAsync(
            It.IsAny<DistributionHistory>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Rule Evaluation - Invalid Conditions

    [Test, AutoData]
    public async Task Handle_WhenConditionJsonIsNull_ShouldSkipRuleAndRecordFailure(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule(DistributionRuleStrategy.FixedTarget, "null")] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());
        RuleType.GetProperty(nameof(DistributionRule.ConditionJson))?.SetValue(rule, "null");

        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        UnitOfWorkMock.Verify(x => x.Set<DistributionHistory>().AddAsync(
            It.IsAny<DistributionHistory>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenConditionJsonHasNoType_ShouldSkipRuleAndRecordFailure(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule(DistributionRuleStrategy.FixedTarget, "{\"min_score\":75}")] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());

        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        UnitOfWorkMock.Verify(x => x.Set<DistributionHistory>().AddAsync(
            It.IsAny<DistributionHistory>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenConditionJsonIsInvalid_ShouldLogErrorAndSkipRule(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule(DistributionRuleStrategy.FixedTarget, "invalid json")] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());

        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error evaluating rule")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        UnitOfWorkMock.Verify(x => x.Set<DistributionHistory>().AddAsync(
            It.IsAny<DistributionHistory>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Target Resolution - Score Based

    [Test, AutoData]
    public async Task Handle_WhenRuleUsesScoreBasedStrategy_ShouldResolveTargetCorrectly(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule(DistributionRuleStrategy.ScoreBased)] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());
        RuleType.GetProperty(nameof(DistributionRule.TargetConfigJson))?.SetValue(rule, 
            "{\"thresholds\":[{\"min_score\":90,\"target\":\"premium\"},{\"min_score\":70,\"target\":\"standard\"}],\"default_target\":\"basic\"}");

        DistributionRequest? createdRequest = null;
        DistributionHistory? createdHistory = null;
        
        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.Set<DistributionRequest>().AddAsync(
                It.IsAny<DistributionRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<DistributionRequest, CancellationToken>((req, _) =>
            {
                createdRequest = req;
            })
            .ReturnsAsync((DistributionRequest _, CancellationToken _) => null!);

        UnitOfWorkMock
            .Setup(x => x.Set<DistributionHistory>().AddAsync(
                It.IsAny<DistributionHistory>(),
                It.IsAny<CancellationToken>()))
            .Callback<DistributionHistory, CancellationToken>((hist, _) =>
            {
                createdHistory = hist;
            })
            .ReturnsAsync((DistributionHistory _, CancellationToken _) => null!);

        await _sut.Handle(@event, CancellationToken.None);

        Assert.That(createdRequest, Is.Not.Null, $"Request was null. History error: {createdHistory?.ErrorMessage}");
        Assert.That(createdRequest!.Target, Is.EqualTo("standard"));
        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenScoreBasedConfigHasNoThresholds_ShouldUseDefaultTarget(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule(DistributionRuleStrategy.ScoreBased)] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());
        RuleType.GetProperty(nameof(DistributionRule.TargetConfigJson))?.SetValue(rule, "{\"default_target\":\"default_system\"}");

        DistributionRequest? createdRequest = null;
        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.Set<DistributionRequest>().AddAsync(
                It.IsAny<DistributionRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<DistributionRequest, CancellationToken>((req, _) => createdRequest = req)
            .ReturnsAsync((DistributionRequest _, CancellationToken _) => null!);

        await _sut.Handle(@event, CancellationToken.None);

        Assert.That(createdRequest, Is.Not.Null);
        Assert.That(createdRequest!.Target, Is.EqualTo("default_system"));
        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenScoreBasedConfigHasInvalidThresholds_ShouldUseDefaultTarget(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule(DistributionRuleStrategy.ScoreBased)] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());
        RuleType.GetProperty(nameof(DistributionRule.TargetConfigJson))?.SetValue(rule, "{\"thresholds\":\"invalid\",\"default_target\":\"default_system\"}");

        DistributionRequest? createdRequest = null;
        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.Set<DistributionRequest>().AddAsync(
                It.IsAny<DistributionRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<DistributionRequest, CancellationToken>((req, _) => createdRequest = req)
            .ReturnsAsync((DistributionRequest _, CancellationToken _) => null!);

        await _sut.Handle(@event, CancellationToken.None);

        Assert.That(createdRequest, Is.Not.Null);
        Assert.That(createdRequest!.Target, Is.EqualTo("default_system"));
        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenScoreBasedConfigUsesListObjectType_ShouldResolveTargetCorrectly(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule(DistributionRuleStrategy.ScoreBased)] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());

        var thresholdsList = new List<object>
        {
            new Dictionary<string, object> { { "min_score", 90 }, { "target", "premium" } },
            new Dictionary<string, object> { { "min_score", 70 }, { "target", "standard" } }
        };
        var targetConfig = new Dictionary<string, object>
        {
            { "thresholds", thresholdsList },
            { "default_target", "basic" }
        };
        var targetConfigJson = System.Text.Json.JsonSerializer.Serialize(targetConfig);
        RuleType.GetProperty(nameof(DistributionRule.TargetConfigJson))?.SetValue(rule, targetConfigJson);

        DistributionRequest? createdRequest = null;
        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.Set<DistributionRequest>().AddAsync(
                It.IsAny<DistributionRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<DistributionRequest, CancellationToken>((req, _) => createdRequest = req)
            .ReturnsAsync((DistributionRequest _, CancellationToken _) => null!);

        await _sut.Handle(@event, CancellationToken.None);

        Assert.That(createdRequest, Is.Not.Null);
        Assert.That(createdRequest!.Target, Is.EqualTo("standard"));
        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenScoreBasedConfigHasInvalidThresholdsType_ShouldUseDefaultTarget(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule(DistributionRuleStrategy.ScoreBased)] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());

        var targetConfig = new Dictionary<string, object>
        {
            { "thresholds", "invalid_type" },
            { "default_target", "default_system" }
        };
        var targetConfigJson = System.Text.Json.JsonSerializer.Serialize(targetConfig);
        RuleType.GetProperty(nameof(DistributionRule.TargetConfigJson))?.SetValue(rule, targetConfigJson);

        DistributionRequest? createdRequest = null;
        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.Set<DistributionRequest>().AddAsync(
                It.IsAny<DistributionRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<DistributionRequest, CancellationToken>((req, _) => createdRequest = req)
            .ReturnsAsync((DistributionRequest _, CancellationToken _) => null!);

        await _sut.Handle(@event, CancellationToken.None);

        Assert.That(createdRequest, Is.Not.Null);
        Assert.That(createdRequest!.Target, Is.EqualTo("default_system"));
        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenScoreBasedThresholdHasNoMinScore_ShouldUseZeroAndResolveTarget(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule(DistributionRuleStrategy.ScoreBased)] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());

        var thresholdsList = new List<object>
        {
            new Dictionary<string, object> { { "target", "specialTarget" } }
        };
        var targetConfig = new Dictionary<string, object>
        {
            { "thresholds", thresholdsList },
            { "default_target", "default" }
        };
        var targetConfigJson = System.Text.Json.JsonSerializer.Serialize(targetConfig);
        RuleType.GetProperty(nameof(DistributionRule.TargetConfigJson))?.SetValue(rule, targetConfigJson);

        DistributionRequest? createdRequest = null;
        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.Set<DistributionRequest>().AddAsync(
                It.IsAny<DistributionRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<DistributionRequest, CancellationToken>((req, _) => createdRequest = req)
            .ReturnsAsync((DistributionRequest _, CancellationToken _) => null!);

        await _sut.Handle(@event, CancellationToken.None);

        Assert.That(createdRequest, Is.Not.Null);
        Assert.That(createdRequest!.Target, Is.EqualTo("specialTarget"));
        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenScoreBasedThresholdHasTargetNull_ShouldUseDefaultTarget(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule(DistributionRuleStrategy.ScoreBased)] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());

        var thresholdsList = new List<object>
        {
            new Dictionary<string, object> { { "min_score", 70 }, { "target", null! } }
        };
        var targetConfig = new Dictionary<string, object>
        {
            { "thresholds", thresholdsList },
            { "default_target", "fallbackTarget" }
        };
        var targetConfigJson = System.Text.Json.JsonSerializer.Serialize(targetConfig);
        RuleType.GetProperty(nameof(DistributionRule.TargetConfigJson))?.SetValue(rule, targetConfigJson);

        DistributionRequest? createdRequest = null;
        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.Set<DistributionRequest>().AddAsync(
                It.IsAny<DistributionRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<DistributionRequest, CancellationToken>((req, _) => createdRequest = req)
            .ReturnsAsync((DistributionRequest _, CancellationToken _) => null!);

        await _sut.Handle(@event, CancellationToken.None);

        Assert.That(createdRequest, Is.Not.Null);
        Assert.That(createdRequest!.Target, Is.EqualTo("fallbackTarget"));
        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenScoreBasedConfigHasNoDefaultTargetAndNoMatchingThreshold_ShouldRecordFailure(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule(DistributionRuleStrategy.ScoreBased)] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());

        var thresholdsList = new List<object>
        {
            new Dictionary<string, object> { { "min_score", 90 }, { "target", "premium" } }
        };
        var targetConfig = new Dictionary<string, object>
        {
            { "thresholds", thresholdsList }
        };
        var targetConfigJson = System.Text.Json.JsonSerializer.Serialize(targetConfig);
        RuleType.GetProperty(nameof(DistributionRule.TargetConfigJson))?.SetValue(rule, targetConfigJson);

        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        UnitOfWorkMock.Verify(x => x.Set<DistributionHistory>().AddAsync(
            It.IsAny<DistributionHistory>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Target Resolution - Round Robin

    [Test, AutoData]
    public async Task Handle_WhenRuleUsesRoundRobinStrategy_ShouldResolveTarget(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule(DistributionRuleStrategy.RoundRobin)] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());
        RuleType.GetProperty(nameof(DistributionRule.TargetConfigJson))?.SetValue(rule, 
            "{\"targets\":[\"rep1\",\"rep2\",\"rep3\"]}");

        DistributionRequest? createdRequest = null;
        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.Set<DistributionRequest>().AddAsync(
                It.IsAny<DistributionRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<DistributionRequest, CancellationToken>((req, _) => createdRequest = req)
            .ReturnsAsync((DistributionRequest _, CancellationToken _) => null!);

        await _sut.Handle(@event, CancellationToken.None);

        Assert.That(createdRequest, Is.Not.Null);
        Assert.That(createdRequest!.Target, Is.Not.Null.And.Not.Empty);
        Assert.That(new[] { "rep1", "rep2", "rep3" }, Does.Contain(createdRequest.Target));
        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenRoundRobinConfigHasNoTargets_ShouldRecordFailure(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule(DistributionRuleStrategy.RoundRobin)] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());
        RuleType.GetProperty(nameof(DistributionRule.TargetConfigJson))?.SetValue(rule, "{}");

        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        UnitOfWorkMock.Verify(x => x.Set<DistributionHistory>().AddAsync(
            It.IsAny<DistributionHistory>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenRoundRobinConfigHasEmptyTargets_ShouldRecordFailure(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule(DistributionRuleStrategy.RoundRobin)] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());
        RuleType.GetProperty(nameof(DistributionRule.TargetConfigJson))?.SetValue(rule, "{\"targets\":[]}");

        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        UnitOfWorkMock.Verify(x => x.Set<DistributionHistory>().AddAsync(
            It.IsAny<DistributionHistory>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Target Resolution - Territory and Specialization

    [Test, AutoData]
    public async Task Handle_WhenTerritoryConfigHasNoTerritories_ShouldRecordFailure(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule(DistributionRuleStrategy.Territory)] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());
        RuleType.GetProperty(nameof(DistributionRule.TargetConfigJson))?.SetValue(rule, "{}");

        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        UnitOfWorkMock.Verify(x => x.Set<DistributionHistory>().AddAsync(
            It.IsAny<DistributionHistory>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenSpecializationConfigHasNoSpecializations_ShouldRecordFailure(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule(DistributionRuleStrategy.Specialization)] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());
        RuleType.GetProperty(nameof(DistributionRule.TargetConfigJson))?.SetValue(rule, "{}");

        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        UnitOfWorkMock.Verify(x => x.Set<DistributionHistory>().AddAsync(
            It.IsAny<DistributionHistory>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenSpecializationConfigUsesDictionaryObjectType_ShouldResolveTargetCorrectly(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule(DistributionRuleStrategy.Specialization)] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());

        var specializationsDict = new Dictionary<string, object>
        {
            { "50-100", "mid_market" },
            { "default", "general" }
        };
        var targetConfig = new Dictionary<string, object>
        {
            { "specializations", specializationsDict }
        };
        var targetConfigJson = System.Text.Json.JsonSerializer.Serialize(targetConfig);
        RuleType.GetProperty(nameof(DistributionRule.TargetConfigJson))?.SetValue(rule, targetConfigJson);

        DistributionRequest? createdRequest = null;
        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        UnitOfWorkMock
            .Setup(x => x.Set<DistributionRequest>().AddAsync(
                It.IsAny<DistributionRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<DistributionRequest, CancellationToken>((req, _) => createdRequest = req)
            .ReturnsAsync((DistributionRequest _, CancellationToken _) => null!);

        await _sut.Handle(@event, CancellationToken.None);

        Assert.That(createdRequest, Is.Not.Null);
        Assert.That(createdRequest!.Target, Is.EqualTo("mid_market"));
        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenSpecializationConfigHasInvalidSpecializationsType_ShouldRecordFailure(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule(DistributionRuleStrategy.Specialization)] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());

        var targetConfig = new Dictionary<string, object>
        {
            { "specializations", "invalid_type" }
        };
        var targetConfigJson = System.Text.Json.JsonSerializer.Serialize(targetConfig);
        RuleType.GetProperty(nameof(DistributionRule.TargetConfigJson))?.SetValue(rule, targetConfigJson);

        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        UnitOfWorkMock.Verify(x => x.Set<DistributionHistory>().AddAsync(
            It.IsAny<DistributionHistory>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, AutoData]
    public async Task Handle_WhenSpecializationConfigHasNoDefault_ShouldRecordFailure(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule(DistributionRuleStrategy.Specialization)] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());

        var specializationsDict = new Dictionary<string, object>
        {
            { "50-100", "mid_market" }
        };
        var targetConfig = new Dictionary<string, object>
        {
            { "specializations", specializationsDict }
        };
        var targetConfigJson = System.Text.Json.JsonSerializer.Serialize(targetConfig);
        RuleType.GetProperty(nameof(DistributionRule.TargetConfigJson))?.SetValue(rule, targetConfigJson);

        var enrichedData = new EnrichedData { Industry = "Technology", CompanySize = "200+", Version = 1 };
        @event.GetType().GetProperty(nameof(LeadQualified.EnrichedData))?.SetValue(@event, enrichedData);

        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        UnitOfWorkMock.Verify(x => x.Set<DistributionHistory>().AddAsync(
            It.IsAny<DistributionHistory>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Concurrency

    [Test, AutoData]
    public void Handle_WhenConcurrencyExceptionOccurs_ShouldThrow(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());

        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);
        UnitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            _sut.Handle(@event, CancellationToken.None));
    }

    #endregion

    #region Invalid Target Configuration

    [Test, AutoData]
    public async Task Handle_WhenTargetConfigJsonIsNull_ShouldRecordFailure(
        [WithValidLeadQualifiedEvent] LeadQualified @event,
        [WithValidDistributionRule] DistributionRule rule)
    {
        RuleType.GetProperty(nameof(DistributionRule.Id))?.SetValue(rule, Guid.NewGuid());
        RuleType.GetProperty(nameof(DistributionRule.TargetConfigJson))?.SetValue(rule, "null");

        var rules = new List<DistributionRule> { rule };
        var ruleSetMock = CreateMockDbSet(rules);
        var requestSetMock = CreateMockDbSet(new List<DistributionRequest>());
        var historySetMock = CreateMockDbSet(new List<DistributionHistory>());

        UnitOfWorkMock.Setup(x => x.Set<DistributionRule>()).Returns(ruleSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionRequest>()).Returns(requestSetMock.Object);
        UnitOfWorkMock.Setup(x => x.Set<DistributionHistory>()).Returns(historySetMock.Object);

        await _sut.Handle(@event, CancellationToken.None);

        UnitOfWorkMock.Verify(x => x.Set<DistributionRequest>().AddAsync(
            It.IsAny<DistributionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        UnitOfWorkMock.Verify(x => x.Set<DistributionHistory>().AddAsync(
            It.IsAny<DistributionHistory>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}