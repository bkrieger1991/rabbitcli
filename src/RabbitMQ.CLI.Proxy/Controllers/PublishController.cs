using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RabbitMQ.Library;

namespace RabbitMQ.CLI.Proxy.Controllers
{
    [ApiController]
    [Route("")]
    public class PublishController : ControllerBase
    {
        private const string ExchangeHeaderKey = "X-Exchange";
        private const string RoutingKeyHeaderKey = "X-RoutingKey";
        private const string QueueHeaderKey = "X-Queue";

        private readonly RabbitMqConfiguration _rabbitMqConfig;
        private readonly RabbitMqClient _client;
        private readonly ILogger<PublishController> _logger;

        public PublishController(RabbitMqConfiguration rabbitMqConfig, RabbitMqClient client, ILogger<PublishController> logger)
        {
            _rabbitMqConfig = rabbitMqConfig;
            _client = client;
            _logger = logger;
        }

        [HttpPost("")]
        public async Task<IActionResult> PublishMessageAsync(
            [FromHeader(Name = QueueHeaderKey)] string queue,
            [FromHeader(Name = ExchangeHeaderKey)] string exchange,
            [FromHeader(Name = RoutingKeyHeaderKey)] string routingKey
        )
        {
            await using var ms = new MemoryStream();
            await HttpContext.Request.Body.CopyToAsync(ms);
            var payload = ms.ToArray();

            _logger.LogInformation($"Received Request at POST '/' - Body: {payload.Length} bytes, Headers: X-Queue={queue}; X-Exchange={exchange}; X-RoutingKey={routingKey}");

            try
            {
                _client.SetConfig(new Library.Configuration.RabbitMqConfiguration()
                {
                    WebInterfaceAddress = "localhost",
                    WebInterfacePort = 80,
                    AmqpAddress = _rabbitMqConfig.Host,
                    AmqpPort = _rabbitMqConfig.Port,
                    Username = _rabbitMqConfig.Username,
                    Password = _rabbitMqConfig.Password,
                    VirtualHost = _rabbitMqConfig.VirtualHost
                });

                ValidateExchangeAndQueue(exchange, queue);
                var headerBlacklist = GetHeaderBlacklist();
                headerBlacklist.AddRange(new[] { RoutingKeyHeaderKey, ExchangeHeaderKey, QueueHeaderKey });
                var parameters = GetParameters(HttpContext.Request.Headers, headerBlacklist);

                if (parameters.Count > 0)
                {
                    _logger.LogDebug($"Found {parameters.Count} headers for usage in parameters and message headers");
                    parameters.ToList().ForEach(p => _logger.LogTrace($"{p.Key}={p.Value}"));
                }
                else
                {
                    _logger.LogDebug("No further headers provided for use as parameters or message-headers");
                }

                if (string.IsNullOrWhiteSpace(queue))
                {
                    _logger.LogInformation($"Publishing given payload to exchange {exchange} with routing-key '{routingKey}'");
                    _client.PublishMessageToExchange(exchange, routingKey, payload, parameters);
                }
                else
                {
                    _logger.LogInformation($"Publishing given payload to queue '{queue}' with routing-key '{routingKey}'");
                    _client.PublishMessageToQueue(queue, routingKey, payload, parameters);
                }

                return Accepted(new {Message = "Sucessful published message"});
            }
            catch (Exception e)
            {
                return StatusCode(
                    500,
                    new
                    {
                        Error = e.Message
                    }
                );
            }
        }

        private List<string> GetHeaderBlacklist()
        {
            var blacklist = _rabbitMqConfig.DefaultHeaderBlacklist.Trim(',', ' ')
                + "," + _rabbitMqConfig.HeaderBlacklist.Trim(',', ' ');
            return blacklist
                .Split(",")
                .Select(h => h.Trim())
                .ToList();
        }

        private IDictionary<string, string> GetParameters(
            IHeaderDictionary headers,
            IReadOnlyCollection<string> blacklist
        )
        {
            return headers
                .ToArray()
                .Where(kv => !blacklist.Contains(kv.Key, StringComparer.InvariantCultureIgnoreCase))
                .ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
        }

        private void ValidateExchangeAndQueue(string exchange, string queue)
        {
            if (!string.IsNullOrWhiteSpace(exchange) && !string.IsNullOrWhiteSpace(queue))
            {
                throw new Exception(
                    "Ambiguous definition of queue and exchange. Provide only one of both."
                );
            }

            if (string.IsNullOrWhiteSpace(exchange) && string.IsNullOrWhiteSpace(queue))
            {
                throw new Exception("Neither queue nor exchange defined. One of both is required.");
            }
        }
    }
}