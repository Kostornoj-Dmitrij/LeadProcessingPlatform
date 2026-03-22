using System.Text.Json;
using EnrichmentService.Application;
using EnrichmentService.Host.Extensions;
using EnrichmentService.Host.Middleware;
using EnrichmentService.Host.Options;
using Microsoft.AspNetCore.Http.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using EnrichmentService.Application.Common.Interfaces;
using EnrichmentService.Infrastructure.Background;
using EnrichmentService.Infrastructure.Clients;
using EnrichmentService.Infrastructure.Data;
using EnrichmentService.Infrastructure.EventBus;
using EnrichmentService.Infrastructure.Inbox;
using EnrichmentService.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedKernel.Base;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.Configure<DatabaseOptions>(
    builder.Configuration.GetSection("ConnectionStrings"));
builder.Services.Configure<KafkaOptions>(
    builder.Configuration.GetSection("Kafka"));

builder.Services.AddApplication();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
    options.UseNpgsql(dataSource, b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
        .UseLoggerFactory(sp.GetRequiredService<ILoggerFactory>()));

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

builder.Services.AddOpenTelemetryConfiguration(builder.Configuration, "EnrichmentService");

builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "postgres",
        tags: ["db", "postgres"])
    .AddKafka(
        new ProducerConfig 
        { 
            BootstrapServers = builder.Configuration["Kafka:BootstrapServers"] 
        },
        name: "kafka",
        tags: ["messaging", "kafka"]);

builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "v1";
    config.Title = "Enrichment Service API";
    config.Version = "v1";
    config.Description = "API for enriching B2B leads with external data";
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi(config =>
    {
        config.DocumentTitle = "Enrichment Service API";
        config.Path = "/swagger";
    });
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseMiddleware<GlobalExceptionHandler>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    await app.ApplyMigrationsAsync();
    await WaitForKafkaTopicsAsync(app.Services);
}

app.Run();

async Task WaitForKafkaTopicsAsync(IServiceProvider services)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    var configuration = services.GetRequiredService<IConfiguration>();

    var bootstrapServers = configuration["Kafka:BootstrapServers"];
    var requiredTopics = new[] 
    { 
        "lead-events", 
        "saga-events",
        "enrichment-events",
        "notification-events"
    };

    var maxRetries = 60;
    var retryDelay = TimeSpan.FromSeconds(2);

    using var adminClient = new AdminClientBuilder(new AdminClientConfig
    {
        BootstrapServers = bootstrapServers
    }).Build();

    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            var existingTopics = metadata.Topics
                .Where(t => t.Error.Code == ErrorCode.NoError)
                .Select(t => t.Topic)
                .ToHashSet();

            var missingTopics = requiredTopics.Except(existingTopics).ToList();

            if (!missingTopics.Any())
            {
                logger.LogInformation("All Kafka topics are available");
                return;
            }

            logger.LogInformation("Waiting for topics: {MissingTopics} (attempt {Attempt}/{MaxRetries})", 
                string.Join(", ", missingTopics), attempt, maxRetries);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Kafka not ready: {Message} (attempt {Attempt}/{MaxRetries})", 
                ex.Message, attempt, maxRetries);
        }

        await Task.Delay(retryDelay);
    }

    logger.LogWarning("Not all topics are available after {MaxRetries} attempts. Continuing anyway...", maxRetries);
}