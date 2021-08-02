using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ConsoleTables;
using EasyNetQ.Management.Client.Model;
using Newtonsoft.Json;
using RabbitMQ.CLI.CommandLineOptions;
using RabbitMQ.Library;
using RabbitMQ.Library.Configuration;
using RabbitMQ.Library.Helper;
using Console = Colorful.Console;

namespace RabbitMQ.CLI.Processors
{
    public class QueueProcessor
    {
        private readonly ConfigurationManager _configManager;
        private readonly RabbitMqClient _rmqClient;

        public QueueProcessor(ConfigurationManager configManager, RabbitMqClient rmqClient)
        {
            _configManager = configManager;
            _rmqClient = rmqClient;
        }
        
        public async Task<int> GetQueues(GetQueuesOptions options)
        {
            var config = _configManager.Get(options.ConfigName);
            _rmqClient.SetConfig(config);

            if (!string.IsNullOrWhiteSpace(options.QueueId))
            {
                await OutputSingleQueue(config, null, options.QueueId);
                return 0;
            }

            if (!string.IsNullOrWhiteSpace(options.QueueName))
            {
                await OutputSingleQueue(config, options.QueueName);
                return 0;
            }

            await OutputQueueList(options);
            return 0;
        }

        private async Task OutputSingleQueue(RabbitMqConfiguration config, string queueName = null, string queueHash = null)
        {
            var (queue, bindings) = queueName == null 
                ? await _rmqClient.GetQueueByHash(queueHash)
                : await _rmqClient.GetQueue(queueName);

            Console.WriteLine(JsonConvert.SerializeObject(new
            {
                queue.Name,
                Id = _rmqClient.HashQueueName(queue.Name),
                queue.Consumers,
                queue.Messages,
                queue.MessagesReady,
                queue.MessagesUnacknowledged,
                queue.AutoDelete,
                queue.Durable,
                queue.Node,
                queue.Policy,
                queue.Vhost,
                queue.Memory
            }, Formatting.Indented), ConsoleColors.JsonColor);

            var bindingTable = new ConsoleTable("From", "RoutingKey") { Options = { EnableCount = false } };
            bindings.ToList().ForEach(b => bindingTable.AddRow(b.Source, b.RoutingKey));
            Console.WriteLine("Bindings:", ConsoleColors.HighlightColor);
            bindingTable.Write();
        }

        private async Task OutputQueueList(GetQueuesOptions options)
        {
            var queues = await _rmqClient.GetQueues();

            if (!string.IsNullOrWhiteSpace(options.Filter))
            {
                queues = queues.Where(q => q.Name.Contains(options.Filter)).ToArray();
            }

            if (!string.IsNullOrWhiteSpace(options.Sort))
            {
                var queueType = typeof(Queue);
                var property = queueType.GetProperty(options.Sort, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (property is null)
                {
                    throw new Exception($"A queue does not contain a property with name \"{options.Sort}\"");
                }

                // Sort queue list
                queues = (
                    options.Descending 
                    ? queues.ToList().OrderByDescending(l => property.GetValue(l))
                    : queues.ToList().OrderBy(l => property.GetValue(l))
                ).ToArray();
            }

            if (options.Limit > 0)
            {
                queues = queues.Take(options.Limit).ToArray();
            }

            var table = new ConsoleTable("id", "name", "consumers", "messages") { Options = { EnableCount = false } };
            queues.ToList().ForEach(q => table.AddRow(Hash.GetShortHash(q.Name), q.Name, q.Consumers, q.Messages));
            table.Write();
        }
    }
}