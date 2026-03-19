using System.Text.Json;
using LeadService.Application;
using LeadService.Infrastructure;
using LeadService.Host.Extensions;
using LeadService.Host.Middleware;
using LeadService.Host.Options;
using Microsoft.AspNetCore.Http.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;

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
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddOpenTelemetryConfiguration(builder.Configuration, "LeadService");

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
    config.Title = "Lead Service API";
    config.Version = "v1";
    config.Description = "API for managing B2B leads lifecycle";
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi(config =>
    {
        config.DocumentTitle = "Lead Service API";
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
        "enrichment-events", 
        "scoring-events", 
        "distribution-events", 
        "saga-events",
        "notification-events",
        "lead-events"
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