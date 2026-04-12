using System.Text.Json;
using LoadTests.Host.Infrastructure;
using LoadTests.Host.Scenarios;
using Microsoft.Extensions.Configuration;
using NBomber.CSharp;
using NBomber.Contracts.Stats;
using SharedKernel.Json;
using Spectre.Console;

static IConfiguration LoadConfiguration()
{
    return new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true)
        .AddEnvironmentVariables()
        .Build();
}

var configuration = LoadConfiguration();
var apiGatewayUrl = configuration["ApiGateway:BaseUrl"]
                    ?? throw new InvalidOperationException("ApiGateway:BaseUrl is not configured");
var connectionString = configuration["ConnectionStrings:LeadDb"]
                       ?? throw new InvalidOperationException("ConnectionStrings:LeadDb is not configured");

AnsiConsole.Write(new FigletText("Lead Platform").Color(Color.Blue));
AnsiConsole.Write(new FigletText("Load Test").Color(Color.Blue));

AnsiConsole.MarkupLine($"[grey]API Gateway: {apiGatewayUrl}[/]");
AnsiConsole.WriteLine();

var mode = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("[yellow]Select test mode:[/]")
        .AddChoices(
            "Success Flow Only",
            "Mixed Load",
            "Enrichment Failure Only",
            "Scoring Failure Only",
            "Distribution Failure Only"));

var targetRps = AnsiConsole.Ask($"[yellow]Target RPS (requests per second):[/]", 50);
var durationSeconds = AnsiConsole.Ask($"[yellow]Test duration (seconds):[/]", 60);
var validateAfterTest = AnsiConsole.Confirm("[yellow]Validate consistency after test?[/]");

AnsiConsole.WriteLine();

var generator = new LeadGenerator();

var scenario = mode switch
{
    "Success Flow Only" => SuccessFlowScenario.Create(apiGatewayUrl, targetRps, durationSeconds, generator),
    "Enrichment Failure Only" => EnrichmentFailureScenario.Create(apiGatewayUrl, targetRps, durationSeconds, generator),
    "Scoring Failure Only" => ScoringFailureScenario.Create(apiGatewayUrl, targetRps, durationSeconds, generator),
    "Distribution Failure Only" => DistributionFailureScenario.Create(apiGatewayUrl, targetRps, durationSeconds, generator),
    _ => throw new InvalidOperationException($"Mode {mode} not implemented yet")
};

AnsiConsole.MarkupLine($"[green]Starting test: {mode}[/]");
AnsiConsole.MarkupLine($"[green]Target: {targetRps} RPS for {durationSeconds} seconds[/]");
AnsiConsole.WriteLine();

try
{
    var result = NBomberRunner
        .RegisterScenarios(scenario)
        .WithReportFolder("Reports")
        .WithReportFormats(ReportFormat.Html, ReportFormat.Txt)
        .Run();

    AnsiConsole.WriteLine();
    AnsiConsole.Write(new Rule("[yellow]PHASE 1: LOAD TEST RESULTS[/]").Centered());

    var stats = result.ScenarioStats[0];

    var table = new Table()
        .Border(TableBorder.Rounded)
        .AddColumn("[grey]Metric[/]")
        .AddColumn("[grey]Value[/]");

    table.AddRow("Total Requests", $"{stats.Ok.Request.Count + stats.Fail.Request.Count}");
    table.AddRow("[green]Success Count (202)[/]", $"[green]{stats.Ok.Request.Count}[/]");
    table.AddRow("[red]Failed Count[/]", $"[red]{stats.Fail.Request.Count}[/]");
    table.AddRow("[green]Actual RPS[/]", $"[green]{stats.Ok.Request.RPS:F2}[/]");
    table.AddRow("[blue]P50 Latency[/]", $"[blue]{stats.Ok.Latency.Percent50:F2} ms[/]");
    table.AddRow("[blue]P95 Latency[/]", $"[blue]{stats.Ok.Latency.Percent95:F2} ms[/]");
    table.AddRow("[blue]P99 Latency[/]", $"[blue]{stats.Ok.Latency.Percent99:F2} ms[/]");
    table.AddRow("Leads Created", $"{generator.CreatedLeadIds.Count}");

    AnsiConsole.Write(table);

    if (validateAfterTest && generator.CreatedLeadIds.Count > 0)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[yellow]PHASE 2: WAITING FOR COMPLETION[/]").Centered());

        var validator = new ConsistencyValidator(connectionString, apiGatewayUrl);

        var maxWaitSeconds = Math.Max(300, durationSeconds * 3);
        var completed = await validator.WaitForAllClosedAsync(
            generator.CreatedLeadIds.ToList(), 
            maxWaitSeconds);

        AnsiConsole.MarkupLine(completed
            ? "[green]✓ All leads reached Closed state![/]"
            : "[red]✗ Timeout: Not all leads completed[/]");

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[yellow]PHASE 3: CONSISTENCY VALIDATION[/]").Centered());

        var validationReport = await validator.ValidateAsync(
            generator.CreatedLeadIds.ToList(),
            generator.ExpectedPaths.ToDictionary());

        if (validationReport.IsValid)
        {
            AnsiConsole.MarkupLine("[green]✓ ALL VALIDATIONS PASSED![/]");
            AnsiConsole.MarkupLine($"[green]  - {validationReport.TotalLeads} leads processed[/]");
            AnsiConsole.MarkupLine($"[green]  - {validationReport.CompletedLeads} reached Closed state[/]");
            AnsiConsole.MarkupLine($"[green]  - 0 state transition violations[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[red]✗ VALIDATION FAILED![/]");
            foreach (var error in validationReport.Errors.Take(20))
            {
                AnsiConsole.MarkupLine($"[red]  {error}[/]");
            }
            if (validationReport.Errors.Count > 20)
            {
                AnsiConsole.MarkupLine($"[grey]  ... and {validationReport.Errors.Count - 20} more errors[/]");
            }
        }

        if (validationReport.ProcessingTimes.Count > 0)
        {
            AnsiConsole.WriteLine();
            var timesTable = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Metric")
                .AddColumn("Value (seconds)");

            var avgTime = validationReport.ProcessingTimes.Average(t => t.TotalSeconds);
            var sorted = validationReport.ProcessingTimes.OrderBy(t => t.TotalSeconds).ToList();
            var p50 = sorted[(int)(sorted.Count * 0.5)].TotalSeconds;
            var p95 = sorted[(int)(sorted.Count * 0.95)].TotalSeconds;
            var p99 = sorted[(int)(sorted.Count * 0.99)].TotalSeconds;

            timesTable.AddRow("Average Time to Close", $"{avgTime:F2}");
            timesTable.AddRow("P50 Time to Close", $"{p50:F2}");
            timesTable.AddRow("P95 Time to Close", $"{p95:F2}");
            timesTable.AddRow("P99 Time to Close", $"{p99:F2}");

            AnsiConsole.Write(timesTable);
        }

        var reportPath = Path.Combine("Reports", $"validation_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        await File.WriteAllTextAsync(
            reportPath,
            JsonSerializer.Serialize(validationReport, JsonDefaults.IndentedOptions));
        AnsiConsole.MarkupLine($"[grey]Validation report saved to: {reportPath}[/]");
    }

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine($"[grey]NBomber reports saved to: {Path.Combine(Directory.GetCurrentDirectory(), "Reports")}[/]");
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[red]Error during test: {ex.Message}[/]");
    AnsiConsole.WriteException(ex);
}