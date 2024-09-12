namespace ServiceBusTelemetryException;

using Azure.Messaging.ServiceBus;

public class MessagesReceiver
{
    private readonly ILogger<MessagesReceiver> _logger;
    private readonly ServiceBusConfiguration _serviceBusConfig;
    private readonly ServiceBusClient _serviceBusClient;
    private ServiceBusProcessor? _processor;

    public MessagesReceiver(ILogger<MessagesReceiver> logger, ServiceBusConfiguration serviceBusConfig, ServiceBusClient serviceBusClient)
    {
        _logger = logger;
        _serviceBusConfig = serviceBusConfig;
        _serviceBusClient = serviceBusClient;
    }

    public async Task InitialiseMessageReceivers(CancellationToken cancellationToken)
    {
        _processor = _serviceBusClient.CreateProcessor(
            _serviceBusConfig.TopicName,
            _serviceBusConfig.Subscription,
            new ServiceBusProcessorOptions());
        _processor.ProcessMessageAsync += ProcessMessage;
        _processor.ProcessErrorAsync += MessageErrorHandler;

        await _processor.StartProcessingAsync(cancellationToken);
    }

    private async Task ProcessMessage(ProcessMessageEventArgs args)
    {
        await Task.Delay(1000);
        _logger.LogInformation("Test message received! {args}", args);
    }

    public async Task ResumeMessageProcessing(CancellationToken cancellationToken)
    {
        if (_processor == null)
            return;

        try
        {
            await _processor.StartProcessingAsync(cancellationToken);
            _logger.LogInformation(
                "Resume messages handling.");
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Error during reactivating massages processing. Restart service required!");
        }
    }

    public async Task PauseMessageProcessing(CancellationToken cancellationToken)
    {
        if (_processor == null)
            return;

        try
        {
            await _processor.StopProcessingAsync(cancellationToken);
            _logger.LogInformation("Messages paused.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error during try to pause massages processing");
        }
    }

    private Task MessageErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Unhanded message processor error. {namespace} -> {entity}. Source: {Source}, ",
            args.FullyQualifiedNamespace, args.EntityPath, args.ErrorSource);
        return Task.CompletedTask;
    }

    public async Task DisposeMessageReceivers(CancellationToken cancellationToken)
    {
        if (_processor != null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
        }
    }
}