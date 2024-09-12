using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace ServiceBusTelemetryException
{
    [ApiController]
    [Route("api/debug")]
    [Produces(System.Net.Mime.MediaTypeNames.Application.Json)]
    public class MessageProcessingPauseController : ControllerBase
    {
        private readonly MessagesReceiver _receiver;

        public MessageProcessingPauseController(MessagesReceiver receiver)
        {
            _receiver = receiver;
        }

        [HttpGet]
        [Route("pause-message-processing")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Pause(int timeoutMs)
        {
            // This activity exported with ServiceBuss - > TaskCanceledException
            await _receiver.PauseMessageProcessing(CancellationToken.None);

            await Task.Delay(timeoutMs);

            await _receiver.ResumeMessageProcessing(CancellationToken.None);
            return Ok();
        }
    }
}
