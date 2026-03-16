using System.Text.Json;
using LeadService.Application;
using LeadService.Infrastructure;
using LeadService.Host.Extensions;
using LeadService.Host.Middleware;
using LeadService.Host.Options;
using Microsoft.AspNetCore.Http.Json;
using System.Text.Json.Serialization;

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
        new Confluent.Kafka.ProducerConfig 
        { 
            BootstrapServers = builder.Configuration["Kafka:BootstrapServers"] 
        },
        name: "kafka",
        tags: ["messaging", "kafka"]);

builder.Services.AddScoped<GlobalExceptionHandler>();
builder.Services.AddScoped<RequestLoggingMiddleware>();

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

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionHandler>();

app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    await app.ApplyMigrationsAsync();
}

app.Run();