using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Library;
using RabbitMQ.Library.Configuration;

namespace RabbitMQ.CLI.Proxy.Shared.Controllers
{
    [ApiController]
    [Route("")]
    public class PublishController : ControllerBase
    {
        private const string ExchangeHeaderKey = "X-Exchange";
        private const string RoutingKeyHeaderKey = "X-RoutingKey";
        private const string QueueHeaderKey = "X-Queue";
        private const string VirtualHostHeaderKey = "X-VirtualHost";
        private const string AuthorizationHeaderKey = "Authorization";

        private readonly ProxyConfiguration _rabbitMqConfig;
        private readonly RabbitMqClient _client;
        private readonly ILogger<PublishController> _logger;

        public PublishController(ProxyConfiguration rabbitMqConfig, RabbitMqClient client, ILogger<PublishController> logger)
        {
            _rabbitMqConfig = rabbitMqConfig;
            _client = client;
            _logger = logger;
        }

        [HttpPost("")]
        public async Task<IActionResult> PublishMessageAsync(
            [FromHeader(Name = QueueHeaderKey)] string queue,
            [FromHeader(Name = ExchangeHeaderKey)] string exchange,
            [FromHeader(Name = RoutingKeyHeaderKey)] string routingKey,
            [FromHeader(Name = VirtualHostHeaderKey)] string virtualHost,
            [FromHeader(Name = AuthorizationHeaderKey)] string authorization
        )
        {
            await using var ms = new MemoryStream();
            await HttpContext.Request.Body.CopyToAsync(ms);
            var payload = ms.ToArray();

            _logger.LogInformation($"Received Request at POST '/' - Body: {payload.Length} bytes, Headers: X-Queue={queue}; X-Exchange={exchange}; X-RoutingKey={routingKey}");
            
            try
            {
                // Perform validations
                ValidateExchangeAndQueue(exchange, queue);
                
                var configuration = CreateConfiguration();
                // Check if we should overwrite configured authorization information with request-header
                ApplyPassedAuthorizationIfGiven(authorization, configuration);
                // Check if we should overwrite configured default virtual-host with a given one
                ApplyVirtualHostIfGiven(virtualHost, configuration);
                // Set configuration to client instance
                _client.SetConfig(configuration);

                
                var parameters = GetParameters(HttpContext.Request.Headers);
                LogParameters(parameters);
                PublishToQueueOrExchange(queue, exchange, routingKey, payload, parameters);

                return Accepted(new {Message = "Successful published message"});
            }
            catch (Exception e) when (e.InnerException is AuthenticationFailureException)
            {
                _logger.LogWarning(e, "Authentication failure");
                return Unauthorized(GetErrorResponse(e, "Authentication failure"));
            }
            catch (Exception e) when (e is BrokerUnreachableException)
            {
                _logger.LogError(e, "Connection failure");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, GetErrorResponse(e, "Broker unreachable"));
            }
            catch (Exception e)
            {
                var message = "Unhandled error";
                if (e.InnerException != null)
                {
                    message += $" of type '{e.InnerException?.GetType().Name}'";
                }
                _logger.LogCritical(e, message);
                return StatusCode(StatusCodes.Status500InternalServerError, GetErrorResponse(e, message));
            }
        }

        private void PublishToQueueOrExchange(
            string queue,
            string exchange,
            string routingKey,
            byte[] payload,
            IDictionary<string, string> parameters
        )
        {
            if (string.IsNullOrWhiteSpace(queue))
            {
                _logger.LogInformation(
                    $"Publishing given payload to exchange {exchange} with routing-key '{routingKey}'"
                );
                _client.PublishMessageToExchange(exchange, routingKey, payload, parameters);
            }
            else
            {
                _logger.LogInformation(
                    $"Publishing given payload to queue '{queue}' with routing-key '{routingKey}'"
                );
                _client.PublishMessageToQueue(queue, routingKey, payload, parameters);
            }
        }

        private RabbitMqConfiguration CreateConfiguration()
        {
            var configuration = new RabbitMqConfiguration()
            {
                WebInterfaceAddress = "localhost",
                WebInterfacePort = 80,
                AmqpAddress = _rabbitMqConfig.Host,
                AmqpPort = _rabbitMqConfig.Port,
                Username = _rabbitMqConfig.Username,
                Password = _rabbitMqConfig.Password,
                VirtualHost = _rabbitMqConfig.VirtualHost
            };
            return configuration;
        }

        private static void ApplyVirtualHostIfGiven(string virtualHost, RabbitMqConfiguration configuration)
        {
            if (!string.IsNullOrEmpty(virtualHost))
            {
                configuration.VirtualHost = virtualHost;
            }
        }

        private void ApplyPassedAuthorizationIfGiven(
            string authorization,
            RabbitMqConfiguration configuration
        )
        {
            if (!string.IsNullOrEmpty(authorization))
            {
                (var username, var password) = GetAuthorization(authorization);

                configuration.Username = username;
                configuration.Password = password;
            }
        }

        private void LogParameters(IDictionary<string, string> parameters)
        {
            if (parameters.Count > 0)
            {
                _logger.LogDebug(
                    $"Found {parameters.Count} headers for usage in parameters and message headers"
                );
                parameters.ToList().ForEach(p => _logger.LogTrace($"{p.Key}={p.Value}"));
            }
            else
            {
                _logger.LogDebug(
                    "No further headers provided for use as parameters or message-headers"
                );
            }
        }

        private object GetErrorResponse(Exception e, string messagePrefix)
        {
            return new
            {
                Type = e.GetType().Name,
                Message = $"{messagePrefix}: {e.Message}",
                InnerType = e.InnerException?.GetType().Name,
                InnerMessage = e.InnerException?.Message
            };
        }

        private List<string> GetHeaderBlacklist()
        {
            var defaultHeaderBlacklist = _rabbitMqConfig.DefaultHeaderBlacklist?.Trim(',', ' ') ?? "";
            var additionalHeaderBlacklist = _rabbitMqConfig.HeaderBlacklist?.Trim(',', ' ') ?? "";

            return defaultHeaderBlacklist
                .Split(",")
                .Where(h => h != "")
                // Concat configurable custom blacklist headers
                .Concat(
                    additionalHeaderBlacklist
                        .Split(",")
                        .Where(h => h != "")
                )
                // Concat reserved header-names for routing-key, exchange and queue
                .Concat(
                    new[] { RoutingKeyHeaderKey, ExchangeHeaderKey, QueueHeaderKey }
                )
                .Distinct()
                .Select(h => h.Trim())
                .ToList();
        }

        private IDictionary<string, string> GetParameters(
            IHeaderDictionary headers
        )
        {
            var blacklist = GetHeaderBlacklist();

            return headers
                .ToArray()
                .Where(kv => !blacklist.Contains(kv.Key, StringComparer.InvariantCultureIgnoreCase))
                .ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
        }

        [AssertionMethod]
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

        private (string username, string password) GetAuthorization(string headerValue)
        {
            var decoded = headerValue.Replace("Basic ", "").FromBase64();
            var split = decoded.Split(":");
            return (split[0], split[1]);
        }
    }
}