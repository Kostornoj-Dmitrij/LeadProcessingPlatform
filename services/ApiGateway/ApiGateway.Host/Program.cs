using System.Text.Json;
using System.Text.Json.Serialization;
using SharedHosting.Extensions;
using SharedHosting.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddSharedOpenTelemetry(builder.Configuration, "ApiGateway");

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandler>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseRouting();

app.MapReverseProxy();

app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Service = "ApiGateway",
    Timestamp = DateTime.UtcNow,
    Environment = app.Environment.EnvironmentName
}));

await app.RunAsync();