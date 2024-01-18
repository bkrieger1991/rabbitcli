using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using EasyNetQ.Management.Client;
using EasyNetQ.Management.Client.Model;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Library.Configuration;
using RabbitMQ.Library.Helper;
using RabbitMQ.Library.Models;
// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace RabbitMQ.Library;

public class RabbitMqClient
{
    private readonly IMapper _mapper;
    private readonly ILogger<RabbitMqClient> _logger;
    private ManagementClient _apiClient;
    private RabbitMqConfiguration _config;
    private Vhost _vhost;
    private ConnectionFactory _amqpConnectionFactory;

    public RabbitMqClient(IMapper mapper, ILogger<RabbitMqClient> logger)
    {
        _mapper = mapper;
        _logger = logger;
    }
    
    public RabbitMqClient SetConfig(RabbitMqConfiguration config)
    {
        _config = config;
        _vhost = new Vhost() { Name = _config.Amqp.VirtualHost };

        CreateApiClient();
        CreateAmqpConnectionFactory();

        return this;
    }

    private void CreateApiClient()
    {
        if (_config == null)
        {
            throw new ArgumentException("No configuration values set.");
        }

        _apiClient = new ManagementClient(
            _config.Web.Hostname, 
            _config.Web.Username, 
            _config.Web.Password, 
            _config.Web.Port,
            ssl: _config.Web.Ssl
        );
    }

    private void CreateAmqpConnectionFactory()
    {
        if (_config == null)
        {
            throw new ArgumentException("No configuration values set.");
        }

        var sslOption = new SslOption();
        if (_config.Amqp.IsAmqps)
        {
            sslOption.Enabled = true;
            if (!_config.Amqp.TlsVersion.IsEmpty())
            {
                sslOption.Version = Enum.Parse<SslProtocols>(_config.Amqp.TlsVersion);
            }

            if (!_config.Amqp.TlsServerName.IsEmpty())
            {
                sslOption.ServerName = _config.Amqp.TlsServerName;
            }

            if (_config.Amqp.Unsecure)
            {
                sslOption.CertificateValidationCallback = (_, _, _, _) => true;
            }
        }

        _amqpConnectionFactory = new ConnectionFactory()
        {
            HostName = _config.Amqp.Hostname,
            Password = _config.Amqp.Password,
            UserName = _config.Amqp.Username,
            Port = _config.Amqp.Port,
            VirtualHost = _config.Amqp.VirtualHost,
            Ssl = sslOption
        };
    }

    public async Task<(Queue Queue, Binding[] Bindings)> GetQueue(string name)
    {
        var queue = await _apiClient.GetQueueAsync(name, _vhost);
        var bindings = await _apiClient.GetBindingsForQueueAsync(queue);

        _logger.LogDebug($"Retrieved data for queue: {queue}");
        return (queue, bindings.ToArray());
    }

    public async Task<(Queue Queue, Binding[] Bindings)> GetQueueByHash(string hash)
    {
        return await GetQueue(await GetQueueNameFromHash(hash));
    }

    public async Task<string> GetQueueNameFromHash(string hash)
    {
        var queues = await GetQueues();
        var queueName = queues
            .Select(q => q.Name)
            .FirstOrDefault(name => Hash.GetShortHash(name) == hash);

        if (queueName == null)
        {
            throw new Exception($"No queue found matching to given hash value \"{hash}\"");
        }

        return queueName;
    }

    public async Task<Queue[]> GetQueues()
    {
        var queues = await _apiClient.GetQueuesAsync(_vhost);
        _logger.LogDebug($"Retrieved {queues.Count} queues in total.");
        return queues.ToArray();
    }

    public string HashQueueName(string name)
    {
        return Hash.GetShortHash(name);
    }

    public async Task<int> TransferMessages(
        string sourceQueue,
        string targetQueue,
        string filter,
        int limit,
        bool copy
    )
    {
        using var connection = _amqpConnectionFactory.CreateConnection();
        using var model = connection.CreateModel();

        try
        {
            using var tempExchange = TemporaryExchange.Create(model).BindTo(targetQueue);
            return await IterateMessages(
                sourceQueue,
                filter,
                limit,
                (m, msg) =>
                {
                    tempExchange.Publish(msg.BasicProperties, msg.Body.ToArray(), msg.RoutingKey);
                    if (!copy)
                    {
                        m.BasicAck(msg.DeliveryTag, false);
                    }
                }
            );
        }
        finally
        {
            // Close every connection
            model.Close();
            connection.Close();
        }
    }

    public async Task CreateQueue(
        string name,
        bool durable,
        bool autoDelete,
        QueueCreateArgument[] arguments
    )
    {
        await ValidateArguments(arguments);
        var inputArgs = new InputArguments();
        _logger.LogDebug("Converting input-arguments for queue-creation. Resolving typed values...");
        arguments.ToList().ForEach(a => inputArgs.Add(a.Key, GetTypedValue(a.Value, a.Type)));

        try
        {
            await CreateQueue(name, autoDelete, durable, inputArgs);
        }
        catch (Exception e)
        {
            throw new Exception("Unknown error when trying to create a queue", e);
        }
    }

    public async Task<Exchange[]> GetExchanges()
    {
        var exchanges = await _apiClient.GetExchangesAsync();
        var filtered = exchanges
            .Where(e => e.Vhost == _config.Amqp.VirtualHost)
            .ToArray();

        _logger.LogDebug($"Requested {filtered.Length} exchanges for vhost '{_config.Amqp.VirtualHost}' from API");

        return filtered;
    }

    public async Task<AmqpMessage> GetMessage(string queue, string hash,
        CancellationToken cancellationToken = default)
    {
        AmqpMessage message = null;
        await IterateMessages(
            queue,
            null,
            0,
            (_, msg) =>
            {
                var mappedMessage = _mapper.Map<AmqpMessage>(msg);
                if (mappedMessage.Identifier == hash)
                {
                    message = mappedMessage;
                }
            },
            cancellationToken
        );

        return message;
    }

    public async Task<AmqpMessage[]> GetMessages(
        string queue,
        int limit,
        string filter = null,
        bool acknowledge = false,
        CancellationToken cancellationToken = default
    )
    {
        var messages = new List<AmqpMessage>();
        await IterateMessages(
            queue,
            filter,
            limit,
            (model, msg) =>
            {
                messages.Add(_mapper.Map<AmqpMessage>(msg));
                if (acknowledge)
                {
                    model.BasicAck(msg.DeliveryTag, false);
                }
            },
            cancellationToken
        );

        return messages.ToArray();
    }

    public async Task<int> PurgeMessages(string queue, string filter)
    {
        var count = 0;
        await IterateMessages(
            queue,
            filter,
            0,
            (model, msg) =>
            {
                count++;
                model.BasicAck(msg.DeliveryTag, false);
            });

        return count;
    }

    public async Task PurgeMessage(string queue, string hash)
    {
        await IterateMessages(
            queue,
            null,
            0,
            (model, msg) =>
            {
                var mappedMessage = _mapper.Map<AmqpMessage>(msg);
                if (mappedMessage.Identifier == hash)
                {
                    model.BasicAck(msg.DeliveryTag, false);
                }
            }
        );
    }

    public async Task<Queue> TempCloneQueue(string queueName)
    {
        // Get data and bindings of source-queue
        var queue = await _apiClient.GetQueueAsync(queueName, _vhost);

        if (queue == null)
        {
            throw new Exception(
                "Error cloning into a temporary queue. The source-queue does not exist."
            );
        }
        var bindings = await _apiClient.GetBindingsForQueueAsync(queue);

        // Generate a random named new queue
        var tempQueueName = Randomizer.GenerateWithGuid("RMQM_LV_");
        var tempQueue = await CreateQueue(tempQueueName);
        _logger.LogDebug($"Creating temporary queue: {tempQueueName}...");
        try
        {

            // Assign the bindings to the new queue
            foreach (var binding in bindings)
            {
                // Ignore the AMQP Default Exchange
                if (binding.Source == "")
                {
                    continue;
                }

                // Get source exchange
                var exchange = await _apiClient.GetExchangeAsync(binding.Source, _vhost);

                // Only for bindings between exchange and queue
                if (binding.DestinationType == "queue")
                {
                    var info = new BindingInfo(binding.RoutingKey);
                    _logger.LogDebug($"Create binding [{exchange.Name}] ==({binding.RoutingKey})==> [TempQueue]");
                    await _apiClient.CreateBindingAsync(exchange, tempQueue, info);
                }
            }

            return tempQueue;
        }
        catch (Exception e)
        {
            await DeleteQueue(tempQueue.Name);
            _logger.LogDebug(e, $"Deleting temporary queue due to exception: {e.Message}...");
            throw;
        }
    }

    public async Task UpdateMessage(string queue, AmqpMessage updatedMessage)
    {
        // Create temporary exchange for later publishing a new message to the queue
        using var connection = _amqpConnectionFactory.CreateConnection();
        using var exchangeModel = connection.CreateModel();
        try
        {
            using var tempExchange = TemporaryExchange.Create(exchangeModel).BindTo(queue);
            await IterateMessages(
                queue,
                null,
                0,
                (model, msg) =>
                {
                    var mappedMessage = _mapper.Map<AmqpMessage>(msg);
                    if (mappedMessage.Identifier == updatedMessage.Identifier)
                    {
                        // Build new message based on values in updatedMessage
                        // Get encoding of message to encode the body correct
                        var encoding = TryGetEncoding(msg.BasicProperties.ContentEncoding);

                        tempExchange.Publish(msg.BasicProperties, encoding.GetBytes(updatedMessage.Content), msg.RoutingKey);

                        // Acknowledge "old" message to delete it
                        model.BasicAck(msg.DeliveryTag, false);
                    }
                }
            );
        }
        finally
        {
            exchangeModel.Close();
            connection.Close();
        }
    }

    public async Task DeleteQueue(string name)
    {
        var queue = await _apiClient.GetQueueAsync(name, _vhost);
        if (queue == null)
        {
            _logger.LogDebug("User wanted to delete a queue, that is not contained in the provided virtual-host");
            throw new Exception("Queue not found in virtual host.");
        }

        await _apiClient.DeleteQueueAsync(queue);
    }

    public void PublishMessageToExchange(
        string exchange,
        string routingKey,
        byte[] content,
        IDictionary<string, string> parameters
    )
    {
        _logger.LogInformation($"Connecting to {_amqpConnectionFactory.HostName}:{_amqpConnectionFactory.Port} with user {_amqpConnectionFactory.UserName}...");
        using var connection = _amqpConnectionFactory.CreateConnection();
        using var model = connection.CreateModel();
        // Create IBasicProperties and map given values in parameters into it
        var properties = CreatePublishPropertiesFromParameters(model, parameters);
        _logger.LogDebug($"Publishing message to exchange {exchange}...");
        model.BasicPublish(exchange, routingKey, false, properties, content);
    }

    public void PublishMessageToQueue(
        string queue,
        string routingKey,
        byte[] content,
        IDictionary<string, string> parameters
    )
    {
        _logger.LogInformation($"Connecting to {_amqpConnectionFactory.HostName}:{_amqpConnectionFactory.Port} with user {_amqpConnectionFactory.UserName}...");
        using var connection = _amqpConnectionFactory.CreateConnection();
        using var model = connection.CreateModel();
        using var tempExchange = TemporaryExchange.Create(model).BindTo(queue);

        // Create IBasicProperties and map given values in parameters into it
        var properties = CreatePublishPropertiesFromParameters(model, parameters);
        _logger.LogDebug($"Publishing message to temporary exchange, bound to queue {queue}...");
        tempExchange.Publish(properties, content, routingKey);
    }

    private IBasicProperties CreatePublishPropertiesFromParameters(IModel model, IDictionary<string, string> parameters)
    {
        const string contentTypeKey = "Content-Type";
        var properties = model.CreateBasicProperties();
        properties.ContentEncoding = Encoding.UTF8.WebName;
        properties.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToList()
            .ForEach(p =>
                {
                    var key = $"RMQ-{p.Name}";
                    if (parameters.ContainsKey(key))
                    {
                        p.SetValue(properties, ConvertValue(parameters[key], p.PropertyType));
                        parameters.Remove(key);
                    }
                }
            );
        properties.ContentType = parameters.ContainsKey(contentTypeKey)
            ? parameters[contentTypeKey]
            : "";
        properties.Headers = parameters
            .Where(p => !p.Key.StartsWith("RMQ-") && !string.Equals(p.Key, contentTypeKey, StringComparison.CurrentCultureIgnoreCase))
            .ToDictionary(kv => kv.Key, kv => (object)kv.Value);
        return properties;
    }

    private object ConvertValue(string value, Type type)
    {
        switch (type.Name.ToLower())
        {
            case "string":
                return value;
            case "bool":
            case "boolean":
                return new[] {"true", "1"}.Contains(value);
            case "byte":
                return byte.Parse(value);
            default:
                throw new Exception($"This property type can't be parsed. Type: {type.Name}, Value: {value}");
        }
    }

    private Task<int> IterateMessages(
        string queue,
        string filter,
        int limit,
        Action<IModel, BasicGetResult> callback,
        CancellationToken cancellationToken = default
    )
    {
        return Task.Run(() =>
        {
            using var connection = _amqpConnectionFactory.CreateConnection();
            using var model = connection.CreateModel();
            BasicGetResult msg;
            var messageCount = 0;
            var passedCount = 0;

            _logger.LogDebug($"Iterating messages of queue '{queue}'. Starting BasicGet's...");
            do
            {
                msg = model.BasicGet(queue, false);
                if (msg != null && MatchesFilter(msg, filter))
                {
                    passedCount++;
                    _logger.LogDebug($"[{msg.DeliveryTag}] filter passed, calling callback...");
                    callback(model, msg);
                }
                messageCount++;
                if (limit != 0 && passedCount >= limit)
                {
                    _logger.LogDebug("Limit of messages to process reached, leaving...");
                    break;
                }
            } while (msg != null && cancellationToken.IsCancellationRequested == false);

            _logger.LogDebug($"Iterator finished. Filtered {passedCount} messages. Ignored {(messageCount - 1) - passedCount} messages, since they did not pass the filter");

            model.Close();
            connection.Close();

            return messageCount;
        });
    }

    private bool MatchesFilter(BasicGetResult msg, string filter)
    {
        if (filter == null)
        {
            return true;
        }

        const string propertySearchPrefix = "properties:";
        const string headerSearchPrefix = "headers:";

        // Special ways to filter with a google-like syntax...
        // prefixed search with properties: will search all property fields
        // prefixed search with headers: will search all headers
        if (filter.StartsWith(propertySearchPrefix, true, CultureInfo.InvariantCulture))
        {
            // Check if any property contains the text behind the prefix
            filter = filter.Substring(propertySearchPrefix.Length);
            return MatchesPropertyFilter(msg, filter);
        }
        if (filter.StartsWith(headerSearchPrefix, true, CultureInfo.InvariantCulture))
        {
            // Check if any header contains the text behind the prefix
            filter = filter.Substring(headerSearchPrefix.Length);
            return MatchesHeaderFilter(msg, filter);
        }

        // Default case: check the content
        var body = Encoding.UTF8.GetString(msg.Body.ToArray());
        return body.Contains(filter, StringComparison.InvariantCultureIgnoreCase);
    }

    private bool MatchesHeaderFilter(BasicGetResult msg, string filter)
    {
        return msg.BasicProperties.Headers.Any(
            kv => RabbitMqValueHelper.ConvertHeaderValue(kv.Value)
                .Contains(filter, StringComparison.InvariantCultureIgnoreCase)
        );
    }

    private bool MatchesPropertyFilter(BasicGetResult msg, string filter)
    {
        // List of properties to check
        var props = new[] { "AppId", "ClusterId", "ContentEncoding", "ContentType", "CorrelationId", "Expiration", "MessageId", "ReplyTo" };

        // Check if any property-value of basicproperties contains the filter
        return props.Any(
            prop =>
            {
                var value = typeof(IBasicProperties)
                        .GetProperty(prop)?
                        .GetValue(msg.BasicProperties)
                    as string;
                return value?.Contains(filter, StringComparison.InvariantCultureIgnoreCase) ?? false;
            }
        );
    }

    private async Task<Queue> CreateQueue(
        string name,
        bool autoDelete = false,
        bool durable = false,
        InputArguments inputArgs = null
    )
    {
        inputArgs ??= new InputArguments();
        var queueInfo = new QueueInfo(name, autoDelete, durable, inputArgs);

        return await _apiClient.CreateQueueAsync(queueInfo, _vhost);
    }

    private Encoding TryGetEncoding(string name, Encoding fallback = null)
    {
        try
        {
            return Encoding.GetEncoding(name);
        }
        catch
        {
            return fallback ?? Encoding.UTF8;
        }
    }

    private async Task ValidateArguments(QueueCreateArgument[] arguments)
    {
        _logger.LogDebug("Validating attributes for queue-creation...");
        // Rule 1: no deadletter-routing-key without deadletter-exchange
        if (arguments.Any(a => a.Key == "x-dead-letter-routing-key")
            && arguments.All(a => a.Key != "x-dead-letter-exchange"))
        {
            _logger.LogDebug("User provided deadletter-routingkey without a deadletter-exchange");
            throw new Exception("Deadletter-Exchange name required when providing an Deadletter-RoutingKey.");
        }

        // Rule 2: queue-mode can only have value "lazy"
        if (arguments.Any(a => a.Key == "x-queue-mode") &&
            arguments.FirstOrDefault(a => a.Key == "x-queue-mode")?.Value != "lazy")
        {
            _logger.LogDebug("User provided an unknown queue-mode");
            throw new Exception("Illegal value for Queue-Mode. Only value 'lazy' is allowed.");
        }

        // Rule 3: Deadletter-Exchange must exist
        if (arguments.Any(a => a.Key == "x-dead-letter-exchange"))
        {
            var exchanges = await GetExchanges();
            if (exchanges.All(e => e.Name != arguments.FirstOrDefault(a => a.Key == "x-dead-letter-exchange")?.Value))
            {
                _logger.LogDebug("User provided a deadletter-exchange that does not exist");
                throw new Exception("The exchange you provided in the Deadletter-Exchange does not exist.");
            }
        }
    }

    private object GetTypedValue(string value, string type)
    {
        switch (type)
        {
            case "number":
                if (!int.TryParse(value, out var number))
                {
                    _logger.LogDebug($"Input of user ('{value}') cannot be parsed as number");
                    throw new Exception($"Error parsing input '{value}' as number.");
                }

                return number;
            case "string":
                return value;
            case "boolean":
                var trueValues = new[] { "1", "y", "yes", "ja", "true", "t" };
                var falseValues = new[] { "0", "n", "no", "nein", "false", "f" };
                if (trueValues.Contains(value))
                {
                    return true;
                }
                else if (falseValues.Contains(value))
                {
                    return false;
                }
                else
                {
                    _logger.LogDebug($"Input of user ('{value}') cannot be parsed as boolean");
                    throw new Exception($"Error parsing input '{value}' as boolean. Possible values are: {string.Join(", ", trueValues.Concat(falseValues))}");
                }
            default:
                _logger.LogDebug($"Given value type ('{type}') does not exist");
                throw new Exception($"The given type '{type}' is not implemented.");
        }
    }
}