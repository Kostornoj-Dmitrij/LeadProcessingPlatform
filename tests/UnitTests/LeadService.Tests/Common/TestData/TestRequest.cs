using MediatR;

namespace LeadService.Tests.Common.TestData;

/// <summary>
/// Тестовый класс запроса для тестов поведений
/// </summary>
public class TestRequest : IRequest<Unit>;