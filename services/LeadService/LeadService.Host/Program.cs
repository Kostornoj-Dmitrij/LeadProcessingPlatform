using LeadService.Application;
using LeadService.Infrastructure;
using LeadService.Infrastructure.Data;
using SharedHosting;
using SharedHosting.Extensions;
using SharedHosting.Options;
using AvroSchemas;

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

builder.Services.Configure<DatabaseOptions>(
    builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.Configure<KafkaOptions>(
    builder.Configuration.GetSection(KafkaOptions.SectionName));

builder.Services.AddApplication();
builder.Services.AddSharedDbContext<ApplicationDbContext>(builder.Configuration);

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseSharedHosting();

if (app.Environment.IsDevelopment())
{
    await app.ApplyMigrationsAsync<ApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
    
    await KafkaExtensions.WaitForKafkaTopicsAsync(app.Services, [
        "enrichment-events",
        "scoring-events",
        "distribution-events",
        "saga-events",
        "notification-events",
        "lead-events"
    ]);

    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var schemaRegistry = app.Services.GetRequiredService<Confluent.SchemaRegistry.ISchemaRegistryClient>();
    await SchemaRegistryHelper.RegisterAllSchemasAsync(schemaRegistry, logger);
}

await app.RunAsync();