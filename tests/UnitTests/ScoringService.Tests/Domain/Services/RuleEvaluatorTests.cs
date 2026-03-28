using AutoFixture.NUnit3;
using AvroSchemas.Messages.LeadEvents;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ScoringService.Domain.Entities;
using ScoringService.Domain.Services;

namespace ScoringService.Tests.Domain.Services;

/// <summary>
/// Тесты для RuleEvaluator
/// </summary>
[Category("Domain")]
public class RuleEvaluatorTests
{
    private Mock<ILogger<RuleEvaluator>> _loggerMock = null!;
    private RuleEvaluator _sut = null!;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<RuleEvaluator>>();
        _sut = new RuleEvaluator(_loggerMock.Object);
    }

    [TearDown]
    public void Cleanup()
    {
        _loggerMock.Reset();
    }

    #region EvaluateAsync_AlwaysTrue

    [Test, AutoData]
    public async Task EvaluateAsync_AlwaysTrue_ShouldReturnTrue(
        ScoringRequest request,
        EnrichedDataDto? enrichedData)
    {
        var rule = ScoringRule.Create(
            Guid.NewGuid(),
            "base_rule",
            "{\"type\": \"always_true\"}",
            10);

        var result = await _sut.EvaluateAsync(rule, request, enrichedData);

        Assert.That(result, Is.True);
    }

    #endregion

    #region EvaluateAsync_FieldEquals

    [Test, AutoData]
    public async Task EvaluateAsync_FieldEquals_IndustryMatch_ShouldReturnTrue(
        ScoringRequest request,
        EnrichedDataDto enrichedData)
    {
        enrichedData.Industry = "Technology";
        var rule = ScoringRule.Create(
            Guid.NewGuid(),
            "industry_rule",
            "{\"type\": \"field_equals\", \"field\": \"industry\", \"value\": \"Technology\"}",
            20);

        var result = await _sut.EvaluateAsync(rule, request, enrichedData);

        Assert.That(result, Is.True);
    }

    [Test, AutoData]
    public async Task EvaluateAsync_FieldEquals_IndustryNoMatch_ShouldReturnFalse(
        ScoringRequest request,
        EnrichedDataDto enrichedData)
    {
        enrichedData.Industry = "Healthcare";
        var rule = ScoringRule.Create(
            Guid.NewGuid(),
            "industry_rule",
            "{\"type\": \"field_equals\", \"field\": \"industry\", \"value\": \"Technology\"}",
            20);

        var result = await _sut.EvaluateAsync(rule, request, enrichedData);

        Assert.That(result, Is.False);
    }

    [Test, AutoData]
    public async Task EvaluateAsync_FieldEquals_CompanyNameMatch_ShouldReturnTrue(
        ScoringRequest request,
        EnrichedDataDto? enrichedData)
    {
        request = ScoringRequest.Create(
            request.LeadId,
            "Acme Corporation",
            request.Email,
            request.ContactPerson,
            request.CustomFields);

        var rule = ScoringRule.Create(
            Guid.NewGuid(),
            "company_name_rule",
            "{\"type\": \"field_equals\", \"field\": \"company_name\", \"value\": \"Acme Corporation\"}",
            15);

        var result = await _sut.EvaluateAsync(rule, request, enrichedData);

        Assert.That(result, Is.True);
    }

    [Test, AutoData]
    public async Task EvaluateAsync_FieldEquals_EmailMatch_ShouldReturnTrue(
        ScoringRequest request,
        EnrichedDataDto? enrichedData)
    {
        request = ScoringRequest.Create(
            request.LeadId,
            request.CompanyName,
            "test@example.com",
            request.ContactPerson,
            request.CustomFields);

        var rule = ScoringRule.Create(
            Guid.NewGuid(),
            "email_rule",
            "{\"type\": \"field_equals\", \"field\": \"email\", \"value\": \"test@example.com\"}",
            10);

        var result = await _sut.EvaluateAsync(rule, request, enrichedData);

        Assert.That(result, Is.True);
    }

    [Test, AutoData]
    public async Task EvaluateAsync_FieldEquals_UnknownField_ShouldReturnFalse(
        ScoringRequest request,
        EnrichedDataDto? enrichedData)
    {
        var rule = ScoringRule.Create(
            Guid.NewGuid(),
            "unknown_field_rule",
            "{\"type\": \"field_equals\", \"field\": \"unknown\", \"value\": \"something\"}",
            5);

        var result = await _sut.EvaluateAsync(rule, request, enrichedData);

        Assert.That(result, Is.False);
    }

    [Test, AutoData]
    public async Task EvaluateAsync_FieldEquals_MissingField_ShouldReturnFalse(
        ScoringRequest request,
        EnrichedDataDto? enrichedData)
    {
        var rule = ScoringRule.Create(
            Guid.NewGuid(),
            "missing_field_rule",
            "{\"type\": \"field_equals\", \"value\": \"Technology\"}",
            10);

        var result = await _sut.EvaluateAsync(rule, request, enrichedData);

        Assert.That(result, Is.False);
    }

    [Test, AutoData]
    public async Task EvaluateAsync_FieldEquals_MissingValue_ShouldReturnFalse(
        ScoringRequest request,
        EnrichedDataDto? enrichedData)
    {
        var rule = ScoringRule.Create(
            Guid.NewGuid(),
            "missing_value_rule",
            "{\"type\": \"field_equals\", \"field\": \"industry\"}",
            10);

        var result = await _sut.EvaluateAsync(rule, request, enrichedData);

        Assert.That(result, Is.False);
    }

    #endregion

    #region EvaluateAsync_FieldContains

    [Test, AutoData]
    public async Task EvaluateAsync_FieldContains_IndustryContainsMatch_ShouldReturnTrue(
        ScoringRequest request,
        EnrichedDataDto enrichedData)
    {
        enrichedData.Industry = "Information Technology";
        var rule = ScoringRule.Create(
            Guid.NewGuid(),
            "industry_contains_rule",
            "{\"type\": \"field_contains\", \"field\": \"industry\", \"value\": \"Technology\"}",
            15);

        var result = await _sut.EvaluateAsync(rule, request, enrichedData);

        Assert.That(result, Is.True);
    }

    [Test, AutoData]
    public async Task EvaluateAsync_FieldContains_IndustryNoMatch_ShouldReturnFalse(
        ScoringRequest request,
        EnrichedDataDto enrichedData)
    {
        enrichedData.Industry = "Healthcare";
        var rule = ScoringRule.Create(
            Guid.NewGuid(),
            "industry_contains_rule",
            "{\"type\": \"field_contains\", \"field\": \"industry\", \"value\": \"Technology\"}",
            15);

        var result = await _sut.EvaluateAsync(rule, request, enrichedData);

        Assert.That(result, Is.False);
    }

    [Test, AutoData]
    public async Task EvaluateAsync_FieldContains_CompanyNameContainsMatch_ShouldReturnTrue(
        ScoringRequest request,
        EnrichedDataDto? enrichedData)
    {
        request = ScoringRequest.Create(
            request.LeadId,
            "Acme Corporation Inc",
            request.Email,
            request.ContactPerson,
            request.CustomFields);

        var rule = ScoringRule.Create(
            Guid.NewGuid(),
            "company_contains_rule",
            "{\"type\": \"field_contains\", \"field\": \"company_name\", \"value\": \"Corp\"}",
            10);

        var result = await _sut.EvaluateAsync(rule, request, enrichedData);

        Assert.That(result, Is.True);
    }

    [Test, AutoData]
    public async Task EvaluateAsync_FieldContains_UnknownField_ShouldReturnFalse(
        ScoringRequest request,
        EnrichedDataDto? enrichedData)
    {
        var rule = ScoringRule.Create(
            Guid.NewGuid(),
            "unknown_contains_rule",
            "{\"type\": \"field_contains\", \"field\": \"unknown\", \"value\": \"test\"}",
            5);

        var result = await _sut.EvaluateAsync(rule, request, enrichedData);

        Assert.That(result, Is.False);
    }

    [Test, AutoData]
    public async Task EvaluateAsync_FieldContains_MissingField_ShouldReturnFalse(
        ScoringRequest request,
        EnrichedDataDto? enrichedData)
    {
        var rule = ScoringRule.Create(
            Guid.NewGuid(),
            "missing_field_contains",
            "{\"type\": \"field_contains\", \"value\": \"Technology\"}",
            10);

        var result = await _sut.EvaluateAsync(rule, request, enrichedData);

        Assert.That(result, Is.False);
    }

    #endregion

    #region EvaluateAsync_CustomFieldEquals

    [Test, AutoData]
    public async Task EvaluateAsync_CustomFieldEquals_WithMatch_ShouldReturnTrue(
        ScoringRequest request,
        EnrichedDataDto? enrichedData)
    {
        var customFields = new Dictionary<string, string>
        {
            { "industry", "Technology" },
            { "source", "website" }
        };
        request = ScoringRequest.Create(
            request.LeadId,
            request.CompanyName,
            request.Email,
            request.ContactPerson,
            customFields);

        var rule = ScoringRule.Create(
            Guid.NewGuid(),
            "custom_field_rule",
            "{\"type\": \"custom_field_equals\", \"field_name\": \"industry\", \"value\": \"Technology\"}",
            25);

        var result = await _sut.EvaluateAsync(rule, request, enrichedData);

        Assert.That(result, Is.True);
    }

    [Test, AutoData]
    public async Task EvaluateAsync_CustomFieldEquals_NoMatch_ShouldReturnFalse(
        ScoringRequest request,
        EnrichedDataDto? enrichedData)
    {
        var customFields = new Dictionary<string, string>
        {
            { "industry", "Healthcare" }
        };
        request = ScoringRequest.Create(
            request.LeadId,
            request.CompanyName,
            request.Email,
            request.ContactPerson,
            customFields);

        var rule = ScoringRule.Create(
            Guid.NewGuid(),
            "custom_field_rule",
            "{\"type\": \"custom_field_equals\", \"field_name\": \"industry\", \"value\": \"Technology\"}",
            25);

        var result = await _sut.EvaluateAsync(rule, request, enrichedData);

        Assert.That(result, Is.False);
    }

    [Test, AutoData]
    public async Task EvaluateAsync_CustomFieldEquals_WhenCustomFieldsNull_ShouldReturnFalse(
        ScoringRequest request,
        EnrichedDataDto? enrichedData)
    {
        request = ScoringRequest.Create(
            request.LeadId,
            request.CompanyName,
            request.Email,
            request.ContactPerson);

        var rule = ScoringRule.Create(
            Guid.NewGuid(),
            "custom_field_rule",
            "{\"type\": \"custom_field_equals\", \"field_name\": \"industry\", \"value\": \"Technology\"}",
            25);

        var result = await _sut.EvaluateAsync(rule, request, enrichedData);

        Assert.That(result, Is.False);
    }

    [Test, AutoData]
    public async Task EvaluateAsync_CustomFieldEquals_MissingFieldName_ShouldReturnFalse(
        ScoringRequest request,
        EnrichedDataDto? enrichedData)
    {
        var rule = ScoringRule.Create(
            Guid.NewGuid(),
            "invalid_rule",
            "{\"type\": \"custom_field_equals\", \"value\": \"Technology\"}",
            25);

        var result = await _sut.EvaluateAsync(rule, request, enrichedData);

        Assert.That(result, Is.False);
    }

    [Test, AutoData]
    public async Task EvaluateAsync_CustomFieldEquals_MissingValue_ShouldReturnFalse(
        ScoringRequest request,
        EnrichedDataDto? enrichedData)
    {
        var customFields = new Dictionary<string, string>
        {
            { "industry", "Technology" }
        };
        request = ScoringRequest.Create(
            request.LeadId,
            request.CompanyName,
            request.Email,
            request.ContactPerson,
            customFields);

        var rule = ScoringRule.Create(
            Guid.NewGuid(),
            "missing_value_rule",
            "{\"type\": \"custom_field_equals\", \"field_name\": \"industry\"}",
            25);

        var result = await _sut.EvaluateAsync(rule, request, enrichedData);

        Assert.That(result, Is.False);
    }

    #endregion

    #region EvaluateAsync_ScoreThreshold

    [Test, AutoData]
    public async Task EvaluateAsync_ScoreThreshold_ShouldReturnFalse(
        ScoringRequest request,
        EnrichedDataDto? enrichedData)
    {
        var rule = ScoringRule.Create(
            Guid.NewGuid(),
            "threshold_rule",
            "{\"type\": \"score_threshold\", \"threshold\": 50}",
            0);

        var result = await _sut.EvaluateAsync(rule, request, enrichedData);

        Assert.That(result, Is.False);
    }

    #endregion

    #region EvaluateAsync_UnknownType

    [Test, AutoData]
    public async Task EvaluateAsync_UnknownRuleType_ShouldReturnFalse(
        ScoringRequest request,
        EnrichedDataDto? enrichedData)
    {
        var rule = ScoringRule.Create(
            Guid.NewGuid(),
            "unknown_rule",
            "{\"type\": \"unknown_type\"}",
            10);

        var result = await _sut.EvaluateAsync(rule, request, enrichedData);

        Assert.That(result, Is.False);
    }

    #endregion

    #region EvaluateAsync_InvalidCondition

    [Test, AutoData]
    public async Task EvaluateAsync_InvalidConditionJson_ShouldLogErrorAndReturnFalse(
        ScoringRequest request,
        EnrichedDataDto? enrichedData)
    {
        var rule = ScoringRule.Create(
            Guid.NewGuid(),
            "invalid_rule",
            "invalid json",
            10);

        var result = await _sut.EvaluateAsync(rule, request, enrichedData);

        Assert.That(result, Is.False);
        
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error evaluating rule")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test, AutoData]
    public async Task EvaluateAsync_ValidJsonButNullCondition_ShouldLogWarningAndReturnFalse(
        ScoringRequest request,
        EnrichedDataDto? enrichedData)
    {
        var rule = ScoringRule.Create(
            Guid.NewGuid(),
            "null_condition_rule",
            "null",
            10);

        var result = await _sut.EvaluateAsync(rule, request, enrichedData);

        Assert.That(result, Is.False);
        
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid condition format")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test, AutoData]
    public async Task EvaluateAsync_MissingTypeField_ShouldLogWarningAndReturnFalse(
        ScoringRequest request,
        EnrichedDataDto? enrichedData)
    {
        var rule = ScoringRule.Create(
            Guid.NewGuid(),
            "no_type_rule",
            "{\"field\": \"industry\", \"value\": \"Technology\"}",
            10);

        var result = await _sut.EvaluateAsync(rule, request, enrichedData);

        Assert.That(result, Is.False);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("missing 'type' field")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}