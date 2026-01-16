using System;
using System.Linq;
using System.Security.Cryptography;
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

var services = new ServiceCollection()
    // Add logging with filter for warning/errors - or for verbose enabled: everything.
    .AddLogging(c => c.AddConsole().AddFilter(level => verbose || level >= LogLevel.Warning))
    .AddSingleton<CommandLineProcessor>()
    .AddSingleton<ConfigProcessor>()
    .AddSingleton<MessageProcessor>()
    .AddSingleton<QueueProcessor>()
    .AddSingleton<ProxyProcessor>();
try
{
    services.AddRabbitMqLibraryComponents();
}
catch(CryptographicException ex)
{
    Console.WriteLine("A cryptographic error occurred while initializing the configuration manager.");
    Console.WriteLine("This may be due to a corrupted configuration file or an issue with encryption keys.");
    Console.WriteLine("Please check your configuration file and ensure that your environment is set up correctly.");
    Console.WriteLine("Error details:");
    Console.WriteLine(ex.ToString());
    return;
}
catch (Exception ex)
{
    Console.WriteLine("An error occurred during initialization:");
    Console.WriteLine(ex.ToString());
    return;
}

var provider = services.BuildServiceProvider();
var commandLineProcessor = provider.GetRequiredService<CommandLineProcessor>();
await commandLineProcessor.Execute(args);