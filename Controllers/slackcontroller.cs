using Microsoft.AspNetCore.Mvc;
using SlackNet;
using SlackNet.AspNetCore;

namespace azopenAiChatApi.Controllers;

[ApiController]
[Route("[controller]")]
public class azopenAiChatApiController : ControllerBase
{
        private readonly ISlackRequestHandler _requestHandler;
        private readonly SlackEndpointConfiguration _endpointConfig;
        private readonly ISlackApiClient _slack;
        //private readonly ILogger<azopenAiChatApiController> _logger;
        public azopenAiChatApiController(ISlackRequestHandler requestHandler, SlackEndpointConfiguration endpointConfig, ISlackApiClient slack)
        {
            _requestHandler = requestHandler;
            _endpointConfig = endpointConfig;
            _slack = slack;
        }

        [HttpPost]
        [Route("/slack/events")]
        public async Task<IActionResult> Event(CancellationToken cancellationToken)
        {
            try {
                cancellationToken.ThrowIfCancellationRequested();
                return await _requestHandler.HandleEventRequest(HttpContext.Request, _endpointConfig);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(500, "Request timed out");
            }
        }
}