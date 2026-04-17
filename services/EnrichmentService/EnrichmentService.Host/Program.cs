using EnrichmentService.Application;
using EnrichmentService.Infrastructure;
using EnrichmentService.Infrastructure.Data;
using SharedHosting;
using SharedHosting.Extensions;
using SharedHosting.Options;
using AvroSchemas;
using SharedHosting.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSharedHosting(builder.Configuration, new HostingOptions
    {
        ServiceName = "EnrichmentService",
        Environment = builder.Environment.EnvironmentName,
        EnableSwagger = builder.Environment.IsDevelopment(),
        EnableHealthChecks = true
    }, additionalTelemetrySources:
    [
        "EnrichmentService.EnrichmentProcessor",
        "EnrichmentService.KafkaConsumer"
    ]);

builder.Services.AddTelemetryLogFilters(builder.Configuration);

builder.Services.Configure<DatabaseOptions>(
    builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.Configure<KafkaOptions>(
    builder.Configuration.GetSection(KafkaOptions.SectionName));

builder.Services.AddApplication();

builder.Services.AddSharedDbContext<ApplicationDbContext>(builder.Configuration);

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseSharedHosting();
app.UseMiddleware<TraceDiagnosticsMiddleware>();

if (app.Environment.IsDevelopment())
{
    await app.ApplyMigrationsAsync<ApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

    await KafkaExtensions.WaitForKafkaTopicsAsync(app.Services, [
        KafkaTopics.LeadEvents,
        KafkaTopics.SagaEvents,
        KafkaTopics.EnrichmentEvents,
        KafkaTopics.NotificationEvents
    ]);

    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var schemaRegistry = app.Services.GetRequiredService<Confluent.SchemaRegistry.ISchemaRegistryClient>();
    await SchemaRegistryHelper.RegisterAllSchemasAsync(schemaRegistry, logger);
}

await app.RunAsync();