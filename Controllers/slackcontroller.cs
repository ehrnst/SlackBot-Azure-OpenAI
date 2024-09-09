using Microsoft.AspNetCore.Mvc;
using SlackNet;
using SlackNet.AspNetCore;
using Microsoft.Extensions.Logging;

namespace azopenAiChatApi.Controllers;

[ApiController]
[Route("[controller]")]
public class azopenAiChatApiController : ControllerBase
{
        private readonly ISlackRequestHandler _requestHandler;
        private readonly SlackEndpointConfiguration _endpointConfig;
        private readonly ISlackApiClient _slack;
        private readonly ILogger<azopenAiChatApiController> _logger;

        public azopenAiChatApiController(ISlackRequestHandler requestHandler, SlackEndpointConfiguration endpointConfig, ISlackApiClient slack, ILogger<azopenAiChatApiController> logger)
        {
            _requestHandler = requestHandler;
            _endpointConfig = endpointConfig;
            _slack = slack;
            _logger = logger;
        }

        [HttpPost]
        [Route("/slack/events")]
        public async Task<IActionResult> Event(CancellationToken cancellationToken)
        {
            try {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogInformation(slackEvent.Text);
                return await _requestHandler.HandleEventRequest(HttpContext.Request, _endpointConfig);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(500, "Request timed out");
            }
        }

        [HttpPost]
        [Route("/hello/{name}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Hello(CancellationToken cancellationToken, string name, ILogger<azopenAiChatApiController> logger)
        {
            try {
                cancellationToken.ThrowIfCancellationRequested();
                logger.LogInformation("hello " + name);     
                return Ok("hello " + name);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(500, "Request timed out");
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }
}