using OpenTelemetry;
using System.Diagnostics;

namespace ServiceBusTelemetryException;

public class ActivityFilteringProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        // Look for the custom tag "error.type" in the activity's tags
        if (activity.Source.Name.Contains("Azure.Messaging.ServiceBus.ServiceBusReceiver"))
        {
            if (activity.TagObjects.Any(
                        tag => tag is { Key: "error.type", Value: string and "System.Threading.Tasks.TaskCanceledException" }))
            {
                // Here I can catch Activity with error from service bus
                // place a brake point
                ;

            }
        }
        base.OnEnd(activity);
    }
}
