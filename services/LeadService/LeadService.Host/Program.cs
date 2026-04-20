using LeadService.Application;
using LeadService.Infrastructure;
using LeadService.Infrastructure.Data;
using SharedHosting;
using SharedHosting.Extensions;
using SharedHosting.Options;
using AvroSchemas;
using AvroSchemas.Naming;
using SharedHosting.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSharedHosting(builder.Configuration, new HostingOptions
    {
        ServiceName = "LeadService",
        Environment = builder.Environment.EnvironmentName,
        EnableSwagger = builder.Environment.IsDevelopment(),
        EnableHealthChecks = true
    }, additionalTelemetrySources:
    [
        "LeadService.CustomProcessor"
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
    
    var naming = app.Services.GetRequiredService<INamingConvention>();
    var requiredTopics = new[]
    {
        naming.GetTopicName(KafkaTopics.EnrichmentEventsBase),
        naming.GetTopicName(KafkaTopics.ScoringEventsBase),
        naming.GetTopicName(KafkaTopics.DistributionEventsBase),
        naming.GetTopicName(KafkaTopics.SagaEventsBase),
        naming.GetTopicName(KafkaTopics.NotificationEventsBase),
        naming.GetTopicName(KafkaTopics.LeadEventsBase)
    };
    
    await KafkaExtensions.WaitForKafkaTopicsAsync(app.Services, requiredTopics);

    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var schemaRegistry = app.Services.GetRequiredService<Confluent.SchemaRegistry.ISchemaRegistryClient>();
    await SchemaRegistryHelper.RegisterAllSchemasAsync(schemaRegistry, naming, logger);
}

await app.RunAsync();