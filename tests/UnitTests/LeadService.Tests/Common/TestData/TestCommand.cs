using MediatR;
using SharedKernel.Base;

namespace LeadService.Tests.Common.TestData;

/// <summary>
/// Тестовый класс команды для тестов поведений
/// </summary>
public class TestCommand : IRequest<Unit>, ICommand;