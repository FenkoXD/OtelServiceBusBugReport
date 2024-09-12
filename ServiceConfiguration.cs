namespace ServiceBusTelemetryException;

public record ApplicationInsightsConfiguration(string ConnectionString);

public class ServiceConfiguration
{
    public ServiceBusConfiguration ServiceBus { get; init; } = default!;
    public ApplicationInsightsConfiguration ApplicationInsights { get; init; } = default!;

}

public record ServiceBusConfiguration
{
    public string ConnectionString { get; init; } = default!;
    public string TopicName { get; init; } = default!;
    public string Subscription { get; init; } = default!;
}