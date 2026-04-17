using DistributionService.Application;
using DistributionService.Infrastructure;
using DistributionService.Infrastructure.Data;
using SharedHosting;
using SharedHosting.Extensions;
using SharedHosting.Options;
using AvroSchemas;
using SharedHosting.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSharedHosting(builder.Configuration, new HostingOptions
    {
        ServiceName = "DistributionService",
        Environment = builder.Environment.EnvironmentName,
        EnableSwagger = builder.Environment.IsDevelopment(),
        EnableHealthChecks = true
    }, additionalTelemetrySources:
    [
        "DistributionService.DistributionProcessor"
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

    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DbInitializer.SeedAsync(context, logger);

    await KafkaExtensions.WaitForKafkaTopicsAsync(app.Services, [
        KafkaTopics.LeadEvents,
        KafkaTopics.DistributionEvents,
        KafkaTopics.NotificationEvents
    ]);

    var schemaRegistry = app.Services.GetRequiredService<Confluent.SchemaRegistry.ISchemaRegistryClient>();
    await SchemaRegistryHelper.RegisterAllSchemasAsync(schemaRegistry, logger);
}

await app.RunAsync();