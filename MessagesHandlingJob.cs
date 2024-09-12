namespace ServiceBusTelemetryException;

public class MessagesHandlingJob : IHostedService
{
    private readonly ILogger<MessagesHandlingJob> _logger;
    private readonly MessagesReceiver _receiver;

    public MessagesHandlingJob(ILogger<MessagesHandlingJob> logger, MessagesReceiver receiver)
    {
        _logger = logger;
        _receiver = receiver;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registered hosted service {name}.", nameof(MessagesHandlingJob));

        try
        {
            await _receiver.InitialiseMessageReceivers(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "ServiceBus message receivers initialization error.");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _receiver.DisposeMessageReceivers(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "ServiceBus message receivers shut down error.");
        }
    }
}