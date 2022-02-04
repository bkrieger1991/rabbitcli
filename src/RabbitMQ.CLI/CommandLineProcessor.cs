using System;
using System.Drawing;
using System.Threading.Tasks;
using CommandLine;
using RabbitMQ.CLI.CommandLineOptions;
using RabbitMQ.CLI.Processors;
using Console = Colorful.Console;

namespace RabbitMQ.CLI
{
    public class CommandLineProcessor
    {
        private readonly QueueProcessor _queueProcessor;
        private readonly ConfigProcessor _configProcessor;
        private readonly MessageProcessor _messageProcessor;
        private readonly ProxyProcessor _proxyProcessor;

        public CommandLineProcessor(
            QueueProcessor queueProcessor, 
            ConfigProcessor configProcessor, 
            MessageProcessor messageProcessor,
            ProxyProcessor proxyProcessor
        )
        {
            _queueProcessor = queueProcessor;
            _configProcessor = configProcessor;
            _messageProcessor = messageProcessor;
            _proxyProcessor = proxyProcessor;
        }

        public async Task Execute(string[] args)
        {
            try
            {
                // Gets a bit dirty...
                // TODO: Is there a better way registering all commands?
                await Parser.Default.ParseArguments<
                        AddConfigOptions, 
                        UpdateConfigOptions, 
                        GetConfigOptions,
                        ConfigurePropertyOptions,
                        GetQueuesOptions,
                        GetMessagesOptions,
                        PurgeMessagesOptions,
                        MoveMessagesOptions,
                        EditMessageOptions,
                        ProxyOptions
                >(args).MapResult(
                        (AddConfigOptions o) => _configProcessor.AddConfig(o),
                        (UpdateConfigOptions o) => _configProcessor.UpdateConfig(o),
                        (GetConfigOptions o) => _configProcessor.GetConfigs(o),
                        (ConfigurePropertyOptions o) => _configProcessor.ConfigureProperty(o),
                        (GetQueuesOptions o) => _queueProcessor.GetQueues(o),
                        (GetMessagesOptions o) => _messageProcessor.GetMessages(o),
                        (PurgeMessagesOptions o) => _messageProcessor.PurgeMessages(o),
                        (MoveMessagesOptions o) => _messageProcessor.MoveMessages(o),
                        (EditMessageOptions o) => _messageProcessor.EditMessage(o),
                        (ProxyOptions o) => _proxyProcessor.CreateProxy(o),
                        _ => Task.FromResult(-1)
                    );
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message, ConsoleColors.ErrorColor);
                Console.WriteLine();
                Console.WriteLine("Extended Information:", ConsoleColors.HighlightColor);
                Console.WriteLine(e.ToString(), Color.Tomato);
            }
        }
    }
}