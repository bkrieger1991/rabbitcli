using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using FluentValidation;
using Microsoft.Extensions.Logging;
using RabbitMQ.CLI.CommandLineOptions;
using RabbitMQ.CLI.Processors;
using Console = Colorful.Console;

namespace RabbitMQ.CLI;

public class CommandLineProcessor
{
    private readonly QueueProcessor _queueProcessor;
    private readonly ConfigProcessor _configProcessor;
    private readonly MessageProcessor _messageProcessor;
    private readonly ProxyProcessor _proxyProcessor;
    private readonly ILogger<CommandLineProcessor> _logger;

    public CommandLineProcessor(
        QueueProcessor queueProcessor, 
        ConfigProcessor configProcessor, 
        MessageProcessor messageProcessor,
        ProxyProcessor proxyProcessor,
        ILogger<CommandLineProcessor> logger
    )
    {
        _queueProcessor = queueProcessor;
        _configProcessor = configProcessor;
        _messageProcessor = messageProcessor;
        _proxyProcessor = proxyProcessor;
        _logger = logger;
    }

    public async Task Execute(string[] args)
    {
        // Map CLI-Options (verbs) to corresponding action
        var commandMap = new Dictionary<Type, IOptionExecutorWrapper>()
        {
            { typeof(ConfigOptions), new OptionExecutorWrapper<ConfigOptions>(o => _configProcessor.HandleConfigCommand(o)) },
            { typeof(PropertyOptions), new OptionExecutorWrapper<PropertyOptions>(o => _configProcessor.HandlePropertyCommand(o)) },
            { typeof(QueueOptions), new OptionExecutorWrapper<QueueOptions>(o => _queueProcessor.HandleQueueCommand(o)) },
            { typeof(MessageOptions), new OptionExecutorWrapper<MessageOptions>(o => _messageProcessor.HandleMessageCommand(o)) },
            { typeof(ProxyOptions), new OptionExecutorWrapper<ProxyOptions>(o => _proxyProcessor.CreateProxy(o)) }
        };

        try
        {
            var parseResult = Parser.Default.ParseArguments(args, commandMap.Keys.ToArray());
            foreach (var command in commandMap)
            {
                // Only matching command gets executed, this is handled by ParserResult.WithParsedAsync(...)
                await command.Value.Execute(parseResult);
            }
        }
        catch (ValidationException e)
        {
            Console.WriteLine("You have some errors in the command usage:", ConsoleColors.DefaultColor);
            foreach (var failure in e.Errors)
            {
                Console.WriteLine(failure.ErrorMessage, ConsoleColors.ErrorColor);
            }
            Console.WriteLine();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message, ConsoleColors.ErrorColor);
            Console.WriteLine();
            // Extended information will only be logged, if verbosity is toggled using --verbose option
            _logger.LogInformation(e, "Extended exception information");
        }
    }
}