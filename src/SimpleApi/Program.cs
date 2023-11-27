using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// The service ame and version are used to identify the application in telemetry data
string serviceName = Assembly.GetExecutingAssembly().GetName().Name!;
string serviceVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                            ?? Assembly.GetExecutingAssembly().GetName()?.Version?.ToString() ?? "1.0.0";
                            
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<SimpleApiMetrics>();
builder.Services.AddSingleton(new ActivitySource(serviceName, serviceVersion));

// Configure OpenTelemetry settings, the console exporter
builder.Services.AddOpenTelemetry()
    .ConfigureResource(rb => rb.AddService(
        serviceName,
        serviceNamespace: null,
        serviceVersion,
        serviceInstanceId: null,
        autoGenerateServiceInstanceId: false))
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddMeter(SimpleApiMetrics.Name)
        .AddOtlpExporter((c,r) => {
            // Export every second, but this is for demo purposes!
            r.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 1000;
        })
        .AddPrometheusExporter()
        // Including this will show telemetry in the console
        // but it is very noisy!
        // .AddConsoleExporter()
    )
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddSource(serviceName)
        .AddOtlpExporter()
    );

builder.Logging
    // Uncomment to remove the default console logger
    // .ClearProviders() 
    .AddOpenTelemetry(
    l => {
        l.AddOtlpExporter();
        l.IncludeFormattedMessage = true;
        // Including this will show otel log telemetry in the console
        // l.AddConsoleExporter();
    });

var app = builder.Build();

ILogger logger = app.Logger;

Random random = new();

int RollDice()
{
    return Random.Shared.Next(1, 7);
}

async Task<string> HandleRollDice(string? player, SimpleApiMetrics metrics, HttpContext ctx, ActivitySource source)
{
    var result = RollDice();

    // Simulate some work for demo purposes
    await Task.Delay(random.Next(100, 750));

    if (string.IsNullOrEmpty(player))
    {
        logger.LogInformation("Anonymous player is rolling the dice: {value}", result);
    }
    else
    {
        logger.LogInformation("{player} is rolling the dice: {value}", player, result);
    }
    
    using (var activity = source.StartActivity("RollDice"))
    {
        await Task.Delay(random.Next(100, 150));
    }

    metrics.RecordDiceRoll(result);
    

    return result.ToString(CultureInfo.InvariantCulture);
}

app.MapGet("/rolldice/{player?}", HandleRollDice);

app.MapGet("/explode", _ => throw new Exception("Boom!"));

// Exposes the Prometheus metrics endpoint on /metrics
// You can browse this endpoint to see what metrics are being reported
// We are using the OpenTelemetry Collector to get metrics though,
// not this endpoint.
app.MapPrometheusScrapingEndpoint();

app.Run();

public class SimpleApiMetrics
{
    public static readonly string Name = Assembly.GetExecutingAssembly().GetName().Name!;
    private readonly Counter<long> _counter;

    public SimpleApiMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(Name);
        _counter = meter.CreateCounter<long>("simpleapi.die_roll_count",
            description: "The number of times the die has been rolled",
            unit: "rolls");
    }

    public void RecordDiceRoll(int value)
    {
        _counter.Add(1, new KeyValuePair<string,object?>("value", value));
    }
    
}