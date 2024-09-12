using Azure.Messaging.ServiceBus;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Azure;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ServiceBusTelemetryException;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", false, true)
    .AddEnvironmentVariables();

var configuration = builder.Configuration.Get<ServiceConfiguration>()!;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHostedService<MessagesHandlingJob>();

builder.Services.AddSingleton(x => new MessagesReceiver(
    x.GetRequiredService<ILogger<MessagesReceiver>>(),
    configuration.ServiceBus,
    x.GetRequiredService<ServiceBusClient>()));

builder.Services.AddAzureClients(x =>
    { x.AddServiceBusClient(configuration.ServiceBus.ConnectionString); });

const string BuildName = "Azure.Experimental.EnableActivitySource";

AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);
builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(
        resourceBuilder => resourceBuilder
            .AddService(serviceName: BuildName, serviceInstanceId: Environment.MachineName))
    .WithTracing(resourceBuilder =>
    {
        resourceBuilder
            // Automatically instrument ASP.NET Core requests
            .AddAspNetCoreInstrumentation()
            // Add custom source for tracing
            .AddProcessor(new ActivityFilteringProcessor())
            .AddSource(BuildName)
            // Here we enabled all Azure.* sources,
            // but added filter to drop HTTP client activities that would be duplicates of Azure activities.
            .AddSource("Azure.*")
            .AddHttpClientInstrumentation(o =>
            {
                o.FilterHttpRequestMessage = (_) => Activity.Current?.Parent?.Source?.Name != "Azure.Core.Http";
            })
            .AddAzureMonitorTraceExporter(o =>
            {
                o.ConnectionString = configuration.ApplicationInsights.ConnectionString;
            });
    })
    .WithMetrics(
        meterProviderBuilder => meterProviderBuilder
            .AddAzureMonitorMetricExporter(
                x => x.ConnectionString = configuration.ApplicationInsights.ConnectionString)
            .AddProcessInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter(
                "Microsoft.AspNetCore.Server.Kestrel",
                BuildName));

builder.Logging
    .ClearProviders()
    .AddConsole()
    .AddOpenTelemetry(loggingOptions =>
    {
        loggingOptions.IncludeFormattedMessage = true;
        loggingOptions.IncludeScopes = true;

        loggingOptions.SetResourceBuilder(
            ResourceBuilder.CreateDefault()
                .AddService(
                    BuildName,
                    serviceVersion: "Test",
                    serviceInstanceId: Environment.MachineName,
                    autoGenerateServiceInstanceId: false));
        loggingOptions.AddConsoleExporter();
        loggingOptions.AddAzureMonitorLogExporter(x =>
            x.ConnectionString = configuration.ApplicationInsights.ConnectionString);
    });


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();
