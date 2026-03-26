using EnrichmentService.Application;
using EnrichmentService.Application.Common.Interfaces;
using EnrichmentService.Infrastructure.Background;
using EnrichmentService.Infrastructure.Clients;
using EnrichmentService.Infrastructure.Data;
using EnrichmentService.Infrastructure.EventBus;
using EnrichmentService.Infrastructure.Inbox;
using EnrichmentService.Infrastructure.Outbox;
using SharedHosting;
using SharedHosting.Extensions;
using SharedHosting.Options;
using SharedKernel.Base;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSharedHosting(builder.Configuration, new HostingOptions
{
    ServiceName = "EnrichmentService",
    Environment = builder.Environment.EnvironmentName,
    EnableSwagger = builder.Environment.IsDevelopment(),
    EnableHealthChecks = true
}, additionalTelemetrySources:
[
    "EnrichmentService.EnrichmentProcessor"
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
builder.Services.AddHostedService<OutboxPublisher>();
builder.Services.AddHostedService<EnrichmentProcessor>();
builder.Services.AddHttpClient<IExternalEnrichmentClient, ExternalEnrichmentClient>();

var app = builder.Build();

app.UseSharedHosting();

if (app.Environment.IsDevelopment())
{
    await app.ApplyMigrationsAsync<ApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
    
    await KafkaExtensions.WaitForKafkaTopicsAsync(app.Services, [
        "lead-events",
        "saga-events",
        "enrichment-events",
        "notification-events"
    ]);
}

await app.RunAsync();