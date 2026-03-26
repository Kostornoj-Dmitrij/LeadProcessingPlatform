using ScoringService.Application;
using ScoringService.Application.Common.Interfaces;
using ScoringService.Domain.Services;
using ScoringService.Infrastructure.Background;
using ScoringService.Infrastructure.Data;
using ScoringService.Infrastructure.EventBus;
using ScoringService.Infrastructure.Inbox;
using ScoringService.Infrastructure.Outbox;
using SharedHosting;
using SharedHosting.Extensions;
using SharedHosting.Options;
using SharedKernel.Base;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSharedHosting(builder.Configuration, new HostingOptions
{
    ServiceName = "ScoringService",
    Environment = builder.Environment.EnvironmentName,
    EnableSwagger = builder.Environment.IsDevelopment(),
    EnableHealthChecks = true
}, additionalTelemetrySources:
[
    "ScoringService.ScoringProcessor",
    "ScoringService.RuleEvaluator"
]);

builder.Services.Configure<DatabaseOptions>(
    builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.Configure<KafkaOptions>(
    builder.Configuration.GetSection(KafkaOptions.SectionName));

builder.Services.AddApplication();

builder.Services.AddSharedDbContext<ApplicationDbContext>(builder.Configuration);

builder.Services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<ApplicationDbContext>());
builder.Services.AddScoped<IDomainEventToOutboxConverter, DomainEventToOutboxConverter>();
builder.Services.AddSingleton<KafkaEventBus>();
builder.Services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<KafkaEventBus>());
builder.Services.AddScoped<IInboxStore, InboxStore>();
builder.Services.AddSingleton<IDeadLetterQueue, KafkaDeadLetterQueue>();
builder.Services.AddHostedService<InboxProcessor>();
builder.Services.AddHostedService<KafkaConsumer>();
builder.Services.AddScoped<IKafkaConsumer>(sp =>
    sp.GetServices<IHostedService>().OfType<KafkaConsumer>().FirstOrDefault()
    ?? throw new InvalidOperationException("KafkaConsumer not found"));
builder.Services.AddScoped<IRuleEvaluator, RuleEvaluator>();
builder.Services.AddHostedService<OutboxPublisher>();
builder.Services.AddHostedService<ScoringProcessor>();

var app = builder.Build();

app.UseSharedHosting();

if (app.Environment.IsDevelopment())
{
    await app.ApplyMigrationsAsync<ApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
    
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DbInitializer.SeedAsync(context, logger);
    
    await KafkaExtensions.WaitForKafkaTopicsAsync(app.Services, [
        "lead-events",
        "saga-events",
        "scoring-events",
        "enrichment-events",
        "notification-events"
    ]);
}

await app.RunAsync();