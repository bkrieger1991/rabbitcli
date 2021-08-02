using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using EasyNetQ.Management.Client;
using EasyNetQ.Management.Client.Model;
using RabbitMQ.Client;
using RabbitMQ.Library.Configuration;
using RabbitMQ.Library.Helper;
using RabbitMQ.Library.Models;

namespace RabbitMQ.Library
{
    public class RabbitMqClient
    {
        private readonly IMapper _mapper;
        private ManagementClient _apiClient;
        private RabbitMqConfiguration _config;
        private Vhost _vhost;
        private ConnectionFactory _amqpConnectionFactory;

        public RabbitMqClient(IMapper mapper)
        {
            _mapper = mapper;
        }

        public RabbitMqClient SetConfig(RabbitMqConfiguration config)
        {
            _config = config;
            _vhost = new Vhost() { Name = _config.VirtualHost };

            CreateApiClient();
            CreateAmqpConnectionFactory();

            return this;
        }

        private void CreateApiClient()
        {
            if (_config == null)
            {
                throw new Exception("No configuration values set.");
            }

            _apiClient = new ManagementClient(
                _config.WebInterfaceAddress, 
                _config.Username, 
                _config.Password, 
                _config.WebInterfacePort,
                ssl: _config.Ssl
            );
        }

        private void CreateAmqpConnectionFactory()
        {
            _amqpConnectionFactory = new ConnectionFactory()
            {
                HostName = _config.AmqpAddress,
                Password = _config.Password,
                UserName = _config.Username,
                Port = _config.AmqpPort,
                VirtualHost = _config.VirtualHost
            };
        }

        public async Task<(Queue Queue, Binding[] Bindings)> GetQueue(string name)
        {
            var queue = await _apiClient.GetQueueAsync(name, _vhost);
            var bindings = await _apiClient.GetBindingsForQueueAsync(queue);

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
            Log("Converting input-arguments for queue-creation. Resolving typed values...");
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
                .Where(e => e.Vhost == _config.VirtualHost)
                .ToArray();

            Log($"Requested {filtered.Length} exchanges for vhost '{_config.VirtualHost}' from API");

            return filtered;
        }

        public async Task<AmqpMessage> GetMessage(string queue, string hash,
            CancellationToken cancellationToken = default)
        {
            AmqpMessage message = null;
            await IterateMessages(
                queue,
                null,
                1,
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
            Log($"Creating temporary queue: {tempQueueName}...");
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
                        Log($"Create binding [{exchange.Name}] ==({binding.RoutingKey})==> [TempQueue]");
                        await _apiClient.CreateBindingAsync(exchange, tempQueue, info);
                    }
                }

                return tempQueue;
            }
            catch (Exception e)
            {
                await DeleteQueue(tempQueue.Name);
                Log(e, $"Deleting temporary queue due to exception: {e.Message}...");
                throw;
            }
        }

        public async Task DeleteQueue(string name)
        {
            var queue = await _apiClient.GetQueueAsync(name, _vhost);
            if (queue == null)
            {
                Log("User wanted to delete a queue, that is not contained in the provided virtual-host");
                throw new Exception("Queue not found in virtual host.");
            }

            await _apiClient.DeleteQueueAsync(queue);
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

                Log($"Iterating messages of queue '{queue}'. Starting BasicGet's...");
                do
                {
                    msg = model.BasicGet(queue, false);
                    if (msg != null && MatchesFilter(msg, filter))
                    {
                        passedCount++;
                        Log($"[{msg.DeliveryTag}] filter passed, calling callback...");
                        callback(model, msg);
                    }
                    messageCount++;
                    if (limit != 0 && passedCount >= limit)
                    {
                        Log("Limit of messages to process reached, leaving...");
                        break;
                    }
                } while (msg != null && cancellationToken.IsCancellationRequested == false);

                Log($"Iterator finished. Filtered {passedCount} messages. Ignored {(messageCount - 1) - passedCount} messages, since they did not pass the filter");

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
            if (filter.StartsWith(propertySearchPrefix))
            {
                // Check if any property contains the text behind the prefix
                filter = filter.Substring(propertySearchPrefix.Length);
                return MatchesPropertyFilter(msg, filter);
            }
            if (filter.StartsWith(headerSearchPrefix))
            {
                // Check if any header contains the text behind the prefix
                filter = filter.Substring(headerSearchPrefix.Length);
                return MatchesHeaderFilter(msg, filter);
            }

            // Default case: check the content
            var body = Encoding.UTF8.GetString(msg.Body.ToArray());
            return body.ToLower().Contains(filter.ToLower());
        }

        private bool MatchesHeaderFilter(BasicGetResult msg, string filter)
        {
            return msg.BasicProperties.Headers.Any(
                kv => RabbitMqValueHelper.ConvertHeaderValue(kv.Value).Contains(filter)
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
                    return value?.Contains(filter) ?? false;
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

        private async Task ValidateArguments(QueueCreateArgument[] arguments)
        {
            Log("Validating attributes for queue-creation...");
            // Rule 1: no deadletter-routing-key without deadletter-exchange
            if (arguments.Any(a => a.Key == "x-dead-letter-routing-key")
                && arguments.All(a => a.Key != "x-dead-letter-exchange"))
            {
                Log("User provided deadletter-routingkey without a deadletter-exchange");
                throw new Exception("Deadletter-Exchange name required when providing an Deadletter-RoutingKey.");
            }

            // Rule 2: queue-mode can only have value "lazy"
            if (arguments.Any(a => a.Key == "x-queue-mode") &&
                arguments.FirstOrDefault(a => a.Key == "x-queue-mode")?.Value != "lazy")
            {
                Log("User provided an unknown queue-mode");
                throw new Exception("Illegal value for Queue-Mode. Only value 'lazy' is allowed.");
            }

            // Rule 3: Deadletter-Exchange must exist
            if (arguments.Any(a => a.Key == "x-dead-letter-exchange"))
            {
                var exchanges = await GetExchanges();
                if (exchanges.All(e => e.Name != arguments.FirstOrDefault(a => a.Key == "x-dead-letter-exchange")?.Value))
                {
                    Log("User provided a deadletter-exchange that does not exist");
                    throw new Exception("The exchange you provided in the Deadletter-Exchange does not exist.");
                }
            }
        }

        private object GetTypedValue(string value, string type)
        {
            switch (type)
            {
                case "number":
                    if (!int.TryParse(value, out int number))
                    {
                        Log($"Input of user ('{value}') cannot be parsed as number");
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
                        Log($"Input of user ('{value}') cannot be parsed as boolean");
                        throw new Exception($"Error parsing input '{value}' as boolean. Possible values are: {string.Join(", ", trueValues.Concat(falseValues))}");
                    }
                default:
                    Log($"Given value type ('{type}') does not exist");
                    throw new Exception($"The given type '{type}' is not implemented.");
            }
        }

        private void Log(string message)
        {
            // Debugging...
            // Console.WriteLine(message);
        }

        private void Log(Exception e, string message)
        {
            // Debugging...
            // Console.WriteLine(message);
            // Console.WriteLine(e);
        }
    }
}