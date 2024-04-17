using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.CLI;
using RabbitMQ.CLI.Processors;
using RabbitMQ.Library;

const string verboseArgument = "--verbose";

// Check if args contain a verbosity switch.
var verbose = args.Contains(verboseArgument);
if (verbose)
{
    // Remove it from args to not confuse the commandline-processor.
    var argList = args.ToList();
    argList.Remove(verboseArgument);
    args = argList.ToArray();
}

var provider = new ServiceCollection()
    // Add logging with filter for warning/errors - or for verbose enabled: everything.
    .AddLogging(c => c.AddConsole().AddFilter(level => verbose || level >= LogLevel.Warning))
    .AddRabbitMqLibraryComponents()
    .AddSingleton<CommandLineProcessor>()
    .AddSingleton<ConfigProcessor>()
    .AddSingleton<MessageProcessor>()
    .AddSingleton<QueueProcessor>()
    .AddSingleton<ProxyProcessor>()
    .BuildServiceProvider();

var commandLineProcessor = provider.GetRequiredService<CommandLineProcessor>();
await commandLineProcessor.Execute(args);