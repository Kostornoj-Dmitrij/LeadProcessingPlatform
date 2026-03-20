using ApiGateway.Host.Middleware;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var otlpEndpoint = builder.Configuration["OpenTelemetry:Endpoint"] ?? "http://aspire-dashboard:18889";
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("ApiGateway")
        .AddTelemetrySdk()
        .AddAttributes([
            new KeyValuePair<string, object>("deployment.environment",
                builder.Environment.EnvironmentName)
        ]))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.Filter = httpContext =>
                !httpContext.Request.Path.StartsWithSegments("/health");
            options.RecordException = true;
        })
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint)))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint)));

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