using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// The service ame and version are used to identify the application in telemetry data
string serviceName = Assembly.GetExecutingAssembly().GetName().Name!;
string serviceVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                            ?? Assembly.GetExecutingAssembly().GetName()?.Version?.ToString() ?? "1.0.0";
                            
var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName,
                serviceVersion: serviceVersion,
                serviceInstanceId: null,
                autoGenerateServiceInstanceId: false)
    .AddAttributes(new [] { new KeyValuePair<string, object>("host.name", Environment.MachineName) });

System.Console.WriteLine($"Service Name: {serviceName} Version: {serviceVersion}");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<SimpleApiMetrics>();


// Configure OpenTelemetry settings, the console exporter
builder.Services.AddOpenTelemetry()
    .WithMetrics(m => m
        .ConfigureResource(r => {
            r.AddService(serviceName,
                serviceVersion: null,
                serviceInstanceId: null,
                autoGenerateServiceInstanceId: false);
            r.AddAttributes(new [] { new KeyValuePair<string, object>("host.name", Environment.MachineName) });
        })
        .AddAspNetCoreInstrumentation()
        .AddMeter(SimpleApiMetrics.Name)
        .AddOtlpExporter((c,r) => {
            // Export every second, but this is for demo purposes!
            r.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 1000;
        })
        .AddConsoleExporter()
    );

var app = builder.Build();

ILogger logger = app.Logger;

Random random = new();

int RollDice()
{
    return Random.Shared.Next(1, 7);
}

async Task<string> HandleRollDice(string? player, SimpleApiMetrics metrics)
{
    var result = RollDice();

    await Task.Delay(random.Next(100, 750));

    if (string.IsNullOrEmpty(player))
    {
        logger.LogInformation("Anonymous player is rolling the dice: {result}", result);
    }
    else
    {
        logger.LogInformation("{player} is rolling the dice: {result}", player, result);
    }

    if (player == "james")
        throw new ApplicationException("James is not allowed to play!");

    metrics.Increment();

    return result.ToString(CultureInfo.InvariantCulture);
}

app.MapGet("/rolldice/{player?}", HandleRollDice);

app.Run();


public static class Telemetry
{
    public static readonly string ServiceName = Assembly.GetExecutingAssembly().GetName().Name!;
    
    public static readonly string ServiceVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                            ?? Assembly.GetExecutingAssembly().GetName()?.Version?.ToString() ?? "1.0.0";
    
    static readonly ActivitySource ActivitySource = new ActivitySource(ServiceName, ServiceVersion);
}

public class SimpleApiMetrics
{
    public static readonly string Name = Assembly.GetExecutingAssembly().GetName().Name!;
    private readonly Counter<long> _counter;

    public SimpleApiMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(Name);
        _counter = meter.CreateCounter<long>("simpeapi.mycounter");
    }

    public void Increment()
    {
        _counter.Add(1);
    }
    
}