using System.Text.Json;
using System.Text.Json.Serialization;
using ApiGateway.Host.Middleware;
using SharedHosting.Extensions;
using SharedHosting.Middleware;

const string serviceName = "ApiGateway";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry().WithMetrics(metrics =>
{
    metrics.AddMeter("ApiGateway.Metrics");
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddSharedOpenTelemetry(builder.Configuration, serviceName);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandler>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<YarpMetricsMiddleware>();

app.UseRouting();

app.MapReverseProxy();

app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Service = serviceName,
    Timestamp = DateTime.UtcNow,
    Environment = app.Environment.EnvironmentName
}));

await app.RunAsync();