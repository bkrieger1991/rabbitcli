﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleTables;
using Newtonsoft.Json;
using RabbitMQ.CLI.CommandLineOptions;
using RabbitMQ.Library;
using RabbitMQ.Library.Configuration;
using RabbitMQ.Library.Models;
using Console = Colorful.Console;

namespace RabbitMQ.CLI.Processors
{
    public class MessageProcessor
    {
        private readonly RabbitMqClient _rmqClient;
        private readonly ConfigurationManager _configManager;
        private readonly CancellationTokenSource _cts;

        public MessageProcessor(RabbitMqClient rmqClient, ConfigurationManager configManager)
        {
            _rmqClient = rmqClient;
            _configManager = configManager;
            _cts = new CancellationTokenSource();
        }

        public async Task<int> GetMessages(GetMessagesOptions options)
        {
            var config = _configManager.Get(options.ConfigName);
            _rmqClient.SetConfig(config);

            var queueName = await GetQueueNameFromOptions(options.QueueName, options.QueueId, config);

            if (options.LiveView)
            {
                await StreamMessages(options, queueName, _cts.Token);
                return 0;
            }

            if (!string.IsNullOrWhiteSpace(options.Hash))
            {
                await OutputSingleMessage(options, queueName);
                return 0;
            }

            await OutputMessages(options, queueName);
            return 0;
        }

        public async Task<int> PurgeMessages(PurgeMessagesOptions options)
        {
            var config = _configManager.Get(options.ConfigName);
            _rmqClient.SetConfig(config);
            var queueName = await GetQueueNameFromOptions(options.QueueName, options.QueueId, config);

            if (!string.IsNullOrWhiteSpace(options.Hash))
            {
                await _rmqClient.PurgeMessage(queueName, options.Hash);
                Console.Write("Message purged", ConsoleColors.DefaultColor);
                return 0;
            }

            var count = await _rmqClient.PurgeMessages(queueName, options.Filter);
            Console.WriteLine();
            Console.Write("Messages purged: ", ConsoleColors.DefaultColor);
            Console.WriteLine(count, ConsoleColors.DefaultColor);Console.WriteLine();
            Console.WriteLine();
            return 0;
        }

        public async Task<int> MoveMessages(MoveMessagesOptions options)
        {
            var config = _configManager.Get(options.ConfigName);
            _rmqClient.SetConfig(config);

            var fromName = await GetQueueNameFromOptions(options.FromName, options.FromId, config);
            var toName = await GetQueueNameFromOptions(options.ToName, options.ToId, config);

            if (options.CreateNew)
            {
                if (string.IsNullOrWhiteSpace(options.ToName))
                {
                    throw new Exception("When you want to create a new queue to move messages, you must provide a name for it");
                }
                await _rmqClient.CreateQueue(toName, true, false, new QueueCreateArgument[] {});
            }

            await _rmqClient.TransferMessages(fromName, toName, options.Filter, options.Limit, options.Copy);

            return 0;
        }

        private async Task StreamMessages(GetMessagesOptions options, string queueName, CancellationToken token)
        {
            if (!StreamMessagesWarning())
            {
                Console.WriteLine("Cancelling...", ConsoleColors.DefaultColor);
                return;
            }

            var count = 0;
            Console.WriteLine("Cloning queue with bindings...");
            var tempQueue = await _rmqClient.TempCloneQueue(queueName);
            Console.WriteLine("Queue created: ");
            Console.WriteLine(tempQueue.Name);
            Console.WriteLine("Start reading messages...");

            Console.CancelKeyPress += CancellationHandler;
            while (!token.IsCancellationRequested)
            {
                var messages = await _rmqClient.GetMessages(tempQueue.Name, 0, options.Filter, true, token);

                if(messages.Length > 0) 
                { 
                    var table = DrawMessageTable(options, messages, false);
                    if (count > 0 && count % 20 != 0)
                    {
                        // 1. Split by newline
                        // 2. Remove empty lines
                        // 3. Skip first 3 lines (header)
                        // 4. Take data line and row seperator
                        // 5. Append newline
                        table = string.Join(Environment.NewLine,
                            table.Split(Environment.NewLine.ToCharArray())
                                .Where(t => !string.IsNullOrWhiteSpace(t))
                                .Skip(3)
                                .ToArray()
                        ) + Environment.NewLine;
                    }

                    if(!string.IsNullOrWhiteSpace(table)) 
                    {
                        Console.Write(table);
                        count += messages.Length;
                    }
                }
                // Not pass available token, cause it throws an operation cancelled exception
                await Task.Delay(100, default);
            }

            Console.WriteLine("Deleting temporary queue...");
            await _rmqClient.DeleteQueue(tempQueue.Name);
            Console.WriteLine("Done cleaning up, exiting...");
        }

        private bool StreamMessagesWarning()
        {
            //Console.BackgroundColor = Color.White;
            Console.WriteLine("ATTENTION: Experimental Feature, Read with caution! Not recommended for production use.", Color.Tomato);
            Console.WriteLine("Using the live-view of messages will produce a copy of the target queue, with same exchange-bindings.", Color.CornflowerBlue);
            Console.WriteLine("If somethings unexpected happens, like terminating the process, the queue will remain and receives messages.", Color.CornflowerBlue);
            Console.Write("If you gently cancel this process by hitting ", Color.MediumSpringGreen);
            Console.Write("CTRL + C ", Color.DarkOrange);
            Console.WriteLine("the queue will be deleted and the process quits.", Color.MediumSpringGreen);
            Console.WriteLine();
            Console.WriteLine("For Your Interest", Color.DarkOrange);
            Console.WriteLine("Messages will be consumed and are getting Ack'ed, so no message remains in the queue while reading.", Color.CornflowerBlue);
            Console.WriteLine("You can also define a filter (--filter) to only show relevant messages.", Color.CornflowerBlue);
            Console.WriteLine();
            Console.WriteLine("Do you want to continue?");
            Console.WriteLine("[yes/no]: ", Color.Tomato);
            var answer = Console.ReadLine().ToLower();
            return answer == "yes";
        }

        private void CancellationHandler(object sender, ConsoleCancelEventArgs args)
        {
            Console.WriteLine("Cancelling...but first cleaning up...");
            _cts.Cancel();
            args.Cancel = true;
        }

        private async Task OutputMessages(GetMessagesOptions options, string queueName)
        {
            var messages = await _rmqClient.GetMessages(queueName, options.Limit, options.Filter);

            if (options.ContentOnly)
            {
                var output = JsonConvert.SerializeObject(messages.Select(m => 
                    m.ContentType?.Contains("application/json") == true
                    ? (object) JsonConvert.DeserializeObject<Dictionary<string, object>>(m.Content)
                    : m.Content).ToArray(), Formatting.Indented);

                Console.WriteLine(output);
                return;
            }

            if (options.OutputJsonList)
            {
                var text = JsonConvert.SerializeObject(messages, Formatting.Indented);
                Console.WriteLine(text, ConsoleColors.JsonColor);
                return;
            }

            Console.WriteLine(DrawMessageTable(options, messages, true));
        }

        private async Task OutputSingleMessage(GetMessagesOptions options, string queueName)
        {
            var message = await _rmqClient.GetMessage(queueName, options.Hash);
            if(message is null)
            {
                Console.WriteLine("No message found.");
            }
            if (options.ContentOnly)
            {
                Console.WriteLine(message.Content, ConsoleColors.JsonColor);
            }
            else
            {
                Console.WriteLine(JsonConvert.SerializeObject(message, Formatting.Indented), ConsoleColors.JsonColor);
            }
        }

        private string DrawMessageTable(GetMessagesOptions options, AmqpMessage[] messages, bool showCount = true)
        {
            var columns = new List<string>() {"Hash", "Content-Type", "Content (shorten)", "Exchange"};
            if (options.ShowHeaders)
            {
                columns.Add("Headers");
            }

            var table = new ConsoleTable(columns.ToArray()) {Options = { EnableCount = showCount } };
            messages.ToList().ForEach(m =>
            {
                var values = new List<string>()
                    {m.Identifier, m.ContentType, m.Content.Shorten(30) + "...", m.Fields.Exchange};
                if (options.ShowHeaders)
                {
                    values.Add(JsonConvert.SerializeObject(m.Properties.Headers));
                }

                table.AddRow(values.Select(v => (object)v).ToArray());
            });
            return table.ToString();
        }

        private async Task<string> GetQueueNameFromOptions(string queueName, string queueId, RabbitMqConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(queueName) && string.IsNullOrWhiteSpace(queueId))
            {
                throw new Exception("You must either provide a queue name or a queue id.");
            }

            var name = queueName;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = await _rmqClient.GetQueueNameFromHash(queueId);
            }

            return name;
        }
    }
}