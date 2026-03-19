using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using LeadService.Application.Common.Behaviors;

namespace LeadService.Application;

/// <summary>
/// Регистрация зависимостей слоя Application
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());

            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}