using System;
using System.Collections.Generic;
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

namespace RabbitMQ.CLI.Processors;

public class QueueProcessor
{
    private readonly ConfigurationManager _configManager;
    private readonly RabbitMqClient _rmqClient;

    public QueueProcessor(ConfigurationManager configManager, RabbitMqClient rmqClient)
    {
        _configManager = configManager;
        _rmqClient = rmqClient;
    }
    
    public async Task HandleQueueCommand(QueueOptions options)
    {
        var config = _configManager.Get(options.ConfigName);
        _rmqClient.SetConfig(config);

        var action = options.Action.ToEnum<QueueOptions.Actions>();
        switch (action)
        {
            case QueueOptions.Actions.Get:
                await GetQueues(options);
                break;
        }
    }

    private async Task GetQueues(QueueOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.QueueId))
        {
            await OutputSingleQueue(null, options.QueueId);
            return;
        }

        if (!string.IsNullOrWhiteSpace(options.QueueName))
        {
            await OutputSingleQueue(options.QueueName);
            return;
        }

        await OutputQueueList(options);
    }

    private async Task OutputSingleQueue(string queueName = null, string queueHash = null)
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

    private async Task OutputQueueList(QueueOptions options)
    {
        var queues = await _rmqClient.GetQueues();
        var querySummary = new List<string>()
        {
            $"Fetched {queues.Length} queues in total."
        };
        
        if (!options.Filter.IsEmpty())
        {
            queues = queues
                .Where(q => q.Name.Contains(options.Filter, StringComparison.InvariantCultureIgnoreCase))
                .ToArray();

            querySummary.Add($"Filtered {queues.Length} queues with \"{options.Filter}\".");
        }

        if (options.Exclude.Any())
        {
            queues = queues
                .Where(q => q.Name.ContainsNoneOf(options.Exclude))
                .ToArray();

            querySummary.Add($"Excluding ({string.Join(", ", options.Exclude.ToArray())}): {queues.Length} queues left.");
        }

        if (!string.IsNullOrWhiteSpace(options.Sort))
        {
            var queueType = typeof(Queue);
            var property = queueType.GetProperty(options.Sort, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (property is null)
            {
                throw new ArgumentException($"A queue does not contain a property with name \"{options.Sort}\"");
            }

            // Sort queue list
            queues = (
                options.Descending 
                    ? queues.ToList().OrderByDescending(l => property.GetValue(l))
                    : queues.ToList().OrderBy(l => property.GetValue(l))
            ).ToArray();

            querySummary.Add($"Sorting result by values in {property.Name}, {(options.Descending ? "descending" : "ascending")}.");
        }

        if (options.Limit > 0)
        {
            querySummary.Add($"Limiting result of {queues.Length} by {options.Limit}.");
            queues = queues.Take(options.Limit).ToArray();
        }

        if (options.ShowQueryInfo)
        {
            Console.WriteLine("Query summary:", ConsoleColors.HighlightColor);
            querySummary.ForEach(i => Console.WriteLine($"  - {i}", ConsoleColors.JsonColor));
            Console.WriteLine("");
            Console.WriteLine("Here is your query result:", ConsoleColors.DefaultColor);
            Console.WriteLine("");
        }

        var table = new ConsoleTable("id", "name", "consumers", "messages") { Options = { EnableCount = false } };
        queues.ToList().ForEach(q => table.AddRow(Hash.GetShortHash(q.Name), q.Name, q.Consumers, q.Messages));
        table.Write();
    }
}